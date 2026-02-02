from fastapi import FastAPI, UploadFile, File, HTTPException
from pydantic import BaseModel
from typing import List
from contextlib import asynccontextmanager
import uvicorn
import torch
import torch.nn as nn
from torchvision import models, transforms
from PIL import Image
import os
from datetime import datetime
import asyncio
from rembg import remove

ENABLE_DEBUG_LOGGING = False

try:
    import pillow_heif
    pillow_heif.register_heif_opener()
    print("pillow-heif registered for HEIC/HEIF support.")
except Exception as e:
    print(f"HEIF support not available: {e}")
from sklearn.cluster import KMeans
import numpy as np
import io
import json

# --- Model Loading ---
# Global model instance
resnet_model = None

def get_model():
    global resnet_model
    if resnet_model is None:
        print("Loading ResNet50 model...")
        model = models.resnet50(weights=models.ResNet50_Weights.DEFAULT)
        model.fc = nn.Identity()
        model.eval()
        resnet_model = model
    return resnet_model

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Load model on startup
    get_model()
    yield
    # Clean up on shutdown (if needed)

app = FastAPI(lifespan=lifespan)

# --- Preprocessing ---
# Standard ResNet normalization
tensor_transform = transforms.Compose([
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
])

from PIL import ImageOps

def preprocess_image_smart(image: Image.Image) -> Image.Image:
    """
    Handles transparency (composite over white) and aspect-ratio preserving resize (pad to 224x224).
    Fallback for when client/server side processing didn't happen.
    """
    # 1. Handle Transparency
    if image.mode in ('RGBA', 'LA') or (image.mode == 'P' and 'transparency' in image.info):
        try:
            # Convert to RGBA to ensure we have an alpha channel
            image = image.convert('RGBA')
            # Create a white background
            bg = Image.new("RGB", image.size, (255, 255, 255))
            # Paste the image on the background using alpha as mask
            bg.paste(image, mask=image.split()[3]) # 3 is alpha
            image = bg
        except Exception as e:
            print(f"Transparency handling failed: {e}, falling back to RGB convert")
            image = image.convert('RGB')
    else:
        image = image.convert('RGB')

    # 2. Resize and Pad (Fit to 224x224)
    # This maintains aspect ratio and pads the rest with white
    target_size = (224, 224)
    if image.size != target_size:
        image = ImageOps.pad(image, target_size, color=(255, 255, 255))
    
    return image

# --- DTOs ---
class ClusterRequest(BaseModel):
    features: List[List[float]]
    k: int

class ClusterResponse(BaseModel):
    labels: List[int]
    centroids: List[List[float]]

# --- Endpoints ---


def preprocess_image_task(contents: bytes, filename: str) -> torch.Tensor:
    """
    CPU-bound preprocessing task.
    Returns the preprocessed image tensor (3, 224, 224).
    """
    try:
        input_image = Image.open(io.BytesIO(contents))
        
        # 1. Apply Background Removal (rembg)
        # SKIPPED: Frontend already sends white-padded 224x224 images.
        # Rembg is redundant and risky for AI images or full-frame inputs.
        # try:
        #     print(f"Applying rembg to {filename}...")
        #     input_image = remove(input_image)
        # except Exception as rembg_err:
        #     print(f"Rembg failed for {filename}: {rembg_err}")

        # 2. Main.py Compatible Preprocessing (Resize(256) -> CenterCrop(224))
        # Note: We must match the DB indexing pipeline exactly.
        # main.py does: Resize(256), CenterCrop(224), ToTensor, Normalize
        
        # Standardize strictly to RGB (drops alpha to black if transparent, matching main.py behavior)
        if input_image.mode != 'RGB':
             input_image = input_image.convert('RGB')

        # Define the exact transform from main.py
        preprocess_pipeline = transforms.Compose([
            transforms.Resize(256),
            transforms.CenterCrop(224),
            transforms.ToTensor(),
            transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
        ])
        
        # 3. Transform
        return preprocess_pipeline(input_image)

    except Exception as e:
        print(f"Error processing image {filename}: {e}")
        return None

def run_batch_inference(tensors: List[torch.Tensor]) -> np.ndarray:
    """
    Runs model inference on a batch of tensors.
    """
    if not tensors:
        return np.array([])
    
    # Stack tensors into a single batch: (N, 3, 224, 224)
    input_batch = torch.stack(tensors)
    
    model = get_model()
    with torch.no_grad():
        output = model(input_batch)
    
    return output.numpy()

@app.post("/extract")
async def extract_features(files: List[UploadFile] = File(...)):
    loop = asyncio.get_event_loop()
    MAX_FILE_SIZE = 20 * 1024 * 1024  # 20 MB limit (aligned with Frontend/Dotnet 17MB)
    
    valid_tensors = []

    # 1. Preprocessing (Parallelized I/O + Offloaded CPU tasks)
    for file in files:
        try:
            contents = await file.read()

            if len(contents) > MAX_FILE_SIZE:
                print(f"Skipping {file.filename}: File too large")
                continue
            
            # Offload preprocessing to thread pool
            tensor = await loop.run_in_executor(None, preprocess_image_task, contents, file.filename)
            
            if tensor is not None:
                valid_tensors.append(tensor)

        except Exception as e:
            print(f"Error reading file {file.filename}: {e}")

    if not valid_tensors:
        raise HTTPException(status_code=400, detail="No valid images processed")

    # 2. Batch Inference (Offloaded to thread pool to avoid blocking)
    # Even though inference is fast, for large batches it can block.
    try:
        vectors = await loop.run_in_executor(None, run_batch_inference, valid_tensors)
    except Exception as e:
        print(f"Inference failed: {e}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"Inference failed: {str(e)}")

    if vectors.size == 0:
        raise HTTPException(status_code=500, detail="Inference failed")

    # 3. Average vectors
    avg_vector = np.mean(vectors, axis=0)
    return {"vector": avg_vector.tolist()}

@app.post("/cluster")
def run_clustering(req: ClusterRequest):
    if not req.features:
        raise HTTPException(status_code=400, detail="No features provided")
    
    data_matrix = np.array(req.features)
    
    # Ensure K is valid
    k = req.k
    if len(data_matrix) < k:
        k = max(1, len(data_matrix))

    kmeans = KMeans(n_clusters=k, random_state=42, n_init=10)
    kmeans.fit(data_matrix)
    
    return {
        "labels": kmeans.labels_.tolist(),
        "centroids": kmeans.cluster_centers_.tolist()
    }

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=5001)
