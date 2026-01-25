# Domus Mercatoris - AI Service

This folder contains the **Python-based AI service** used by the Domus Mercatoris platform for image feature extraction and clustering. It is built with **FastAPI** and uses **PyTorch** (ResNet-50) and **Rembg** for image processing.

## 🧠 Core Capabilities

1.  **Feature Extraction:** Uses a pre-trained **ResNet-50** model (final classification layer removed) to convert product images into 2048-dimensional feature vectors.
2.  **Background Removal:** Uses **`rembg`** (U2-Net) to automatically remove backgrounds from user-uploaded images, improving classification accuracy.
3.  **Clustering:** Implements K-Means clustering for grouping similar products.

## 🖼 The "Golden Ratio" Processing Pipeline

To optimize ResNet-50 performance, this service implements the backend portion of our image processing pipeline:

1.  **Input:** Receives a cropped image from the Frontend (or raw image via API).
2.  **Background Removal:** Applies `rembg.remove()` to isolate the object.
3.  **Smart Preprocessing:**
    *   Composites the transparent image over a **white background**.
    *   Resizes to **224x224** pixels.
    *   Preserves **aspect ratio** by padding with white (no distortion).
4.  **Inference:** Feeds the processed image to ResNet-50.

## 🛠 Prerequisites

*   **Python 3.11** (Strict requirement for `rembg` compatibility).
*   **macOS / Linux** (Recommended environment).

## 📦 Installation

1.  **Create a Virtual Environment:**
    From the repository root (`DomusMercatoris`):
    ```bash
    python3.11 -m venv venv
    ```

2.  **Install Dependencies:**
    ```bash
    source venv/bin/activate
    pip install --upgrade pip
    pip install -r AI/requirements.txt
    ```
    *Dependencies include: `fastapi`, `uvicorn`, `torch`, `torchvision`, `rembg`, `pillow`, `numpy`, `scikit-learn`.*

## 🚀 Running the Service

The service runs on port **5001** by default.

### Manual Start (Debug Mode)
```bash
# From project root
source venv/bin/activate
python -m uvicorn AI.api:app --port 5001 --reload
```

### Automatic Start
The .NET backend application (`DomusMercatorisDotnetRest` or MVC app) includes a `PythonRunnerService` that attempts to start this API automatically if it's not running.

## 🐛 Debugging & Logging

**Debug Logging** is enabled by default (`ENABLE_DEBUG_LOGGING = True` in `api.py`).

*   **Log Location:** `AI/AiLogs/`
*   **Contents:**
    *   `YYYYMMDD_..._filename.jpg`: The final preprocessed image seen by ResNet (224x224, white bg).
    *   `YYYYMMDD_..._vector.json`: The output feature vector.

Use these logs to verify that the background removal and resizing are working correctly.

## 🔌 API Endpoints

*   `POST /extract`: Accepts a list of image files, returns feature vectors.
*   `POST /cluster`: Accepts feature vectors and `k`, returns cluster labels and centroids.
