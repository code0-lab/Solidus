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


@app.post("/extract")
async def extract_features(files: List[UploadFile] = File(...)):
    model = get_model()
    vectors = []
    
    for file in files:
        try:
            contents = await file.read()
            input_image = Image.open(io.BytesIO(contents))
            
            # 1. Apply Background Removal (rembg)
            # This is the "Golden Ratio" - crop in Angular, rembg in Python
            try:
                print(f"Applying rembg to {file.filename}...")
                input_image = remove(input_image)
            except Exception as rembg_err:
                print(f"Rembg failed for {file.filename}: {rembg_err}")
                # Continue with original image if rembg fails

            # 2. Smart Preprocessing (Handles resizing to 224x224 & transparency/white bg)
            # rembg outputs RGBA, so this will composite it over white background
            input_image = preprocess_image_smart(input_image)

            if ENABLE_DEBUG_LOGGING:
                try:
                    log_dir = os.path.join(os.path.dirname(__file__), "AiLogs")
                    os.makedirs(log_dir, exist_ok=True)
                    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S_%f")
                    img_log_path = os.path.join(log_dir, f"{timestamp}_{file.filename or 'unknown'}.jpg")
                    # Save as JPEG to verify what the model sees (RGB, white bg)
                    input_image.save(img_log_path, "JPEG")
                    print(f"Logged input image to {img_log_path}")
                except Exception as log_err:
                    print(f"Logging failed: {log_err}")
            
            input_tensor = tensor_transform(input_image)
            input_batch = input_tensor.unsqueeze(0)

            with torch.no_grad():
                output = model(input_batch)
            
            vec = output[0].numpy()
            vectors.append(vec)

            if ENABLE_DEBUG_LOGGING:
                try:
                    vec_log_path = os.path.join(log_dir, f"{timestamp}_{file.filename or 'unknown'}_vector.json")
                    with open(vec_log_path, "w") as f:
                        json.dump(vec.tolist(), f)
                except Exception as log_err:
                    print(f"Vector logging failed: {log_err}")

        except Exception as e:
            print(f"Error processing image: {e}")
            # Continue or raise? For now, skip failed images.
            pass

    if not vectors:
        raise HTTPException(status_code=400, detail="No valid images processed")

    # Average if multiple images
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
