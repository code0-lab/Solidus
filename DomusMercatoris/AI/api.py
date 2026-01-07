from fastapi import FastAPI, UploadFile, File, HTTPException
from pydantic import BaseModel
from typing import List
from contextlib import asynccontextmanager
import uvicorn
import torch
import torch.nn as nn
from torchvision import models, transforms
from PIL import Image
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
preprocess = transforms.Compose([
    transforms.Resize(256),
    transforms.CenterCrop(224),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
])

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
            input_image = Image.open(io.BytesIO(contents)).convert('RGB')
            input_tensor = preprocess(input_image)
            input_batch = input_tensor.unsqueeze(0)

            with torch.no_grad():
                output = model(input_batch)
            
            vectors.append(output[0].numpy())
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
