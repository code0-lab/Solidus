Domus Mercatoris AI Service
===========================

This folder contains the Python-based AI service used by the Domus Mercatoris MVC application for image feature extraction and clustering.

The service is implemented in `api.py` using FastAPI and is started automatically by the .NET MVC app via `PythonRunnerService`.


Prerequisites
-------------

- Python 3 (installed and available as `python3` on your PATH)
- macOS or Linux (commands below use Unix-style paths)
- Git clone of this repository


1. Create the Virtual Environment
---------------------------------

From the repository root (`DomusMercatoris`), create a virtual environment named `venv`:

```bash
cd /path/to/DomusMercatoris
python3 -m venv venv
```

This will create:

- `venv/bin/python` (interpreter the MVC app prefers to use)
- `venv/bin/pip` (package installer)


2. Install Python Dependencies
------------------------------

With the virtual environment created, install the AI service dependencies from `AI/requirements.txt`:

```bash
cd /path/to/DomusMercatoris
venv/bin/python -m pip install --upgrade pip
venv/bin/python -m pip install -r AI/requirements.txt
```

This installs (among others):

- `fastapi`
- `uvicorn`
- `torch`, `torchvision`
- `numpy`, `scikit-learn`
- `pillow`, `pillow-heif`


3. How the MVC App Starts the AI Service
----------------------------------------

The .NET MVC application includes `PythonRunnerService` (`MVC/MVC/Services/PythonRunnerService.cs`) which is responsible for starting the Python API.

Key behavior:

- At startup, the service searches upward from the current working directory to find the project root containing the `AI` folder.
- Once found, it sets:
  - `pythonExecutable` to `<root>/venv/bin/python` if that file exists.
  - If the virtual environment does **not** exist, it falls back to `python3` from the system PATH.
- It runs:
  - `AI/api.py` as the entry point.
  - Working directory is the project root so that relative paths in the Python code behave correctly.
- Standard output and error from the Python process are forwarded into the ASP.NET Core logging system.

For reliable operation, it is recommended to:

- Keep the `AI` folder at the solution root.
- Create and use the `venv` virtual environment as described above.


4. Running the AI Service Manually (for Testing)
-----------------------------------------------

You can also run the AI service manually without starting the MVC app, which is useful for debugging.

From the repository root:

```bash
cd /path/to/DomusMercatoris
source venv/bin/activate
python AI/api.py
```

This will start a FastAPI server via Uvicorn on:

- `http://0.0.0.0:5001`

Endpoints:

- `POST /extract` — accepts image files and returns a feature vector.
- `POST /cluster` — accepts feature vectors and runs K-Means clustering.


5. Example Requests and Responses
---------------------------------

### 5.1. `POST /extract`

Example request using `curl` (sending a single image file):

```bash
curl -X POST "http://localhost:5001/extract" \
  -H "accept: application/json" \
  -H "Content-Type: multipart/form-data" \
  -F "files=@/path/to/image.jpg"
```

Example JSON response (shortened for readability):

```json
{
  "vector": [
    0.012345678,
    -0.03456789,
    0.00123456
    // ... total length 2048 floats
  ]
}
```

The `vector` field is a list of 2048 floating-point values representing the extracted image features.


### 5.2. `POST /cluster`

Example request body:

```json
{
  "features": [
    [0.01, 0.02, 0.03],
    [0.02, 0.01, 0.05],
    [0.10, 0.20, 0.30]
  ],
  "k": 2
}
```

Example request using `curl`:

```bash
curl -X POST "http://localhost:5001/cluster" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "features": [
      [0.01, 0.02, 0.03],
      [0.02, 0.01, 0.05],
      [0.10, 0.20, 0.30]
    ],
    "k": 2
  }'
```

Example JSON response:

```json
{
  "labels": [0, 0, 1],
  "centroids": [
    [0.015, 0.015, 0.04],
    [0.10, 0.20, 0.30]
  ]
}
```

- `labels` is a list assigning each input feature vector to a cluster index.
- `centroids` is a list of cluster centers in the same feature space.


6. Common Issues and Troubleshooting
------------------------------------

**Problem: `ModuleNotFoundError: No module named 'fastapi'` or similar**

- Cause: Dependencies were not installed into the environment that is running `api.py`.
- Fix:
  - Ensure you have created the `venv` at the project root.
  - Run:

    ```bash
    cd /path/to/DomusMercatoris
    venv/bin/python -m pip install -r AI/requirements.txt
    ```

**Problem: MVC logs show “Python service will not start” or API script not found**

- Ensure that:
  - The repository structure still includes the `AI` folder at the root.
  - The MVC app is being run from within the solution (so the root search can find `AI`).


7. Updating Dependencies
------------------------

When you need to add or update Python packages:

1. Modify `AI/requirements.txt` to include the desired packages.
2. Re-install dependencies:

   ```bash
   cd /path/to/DomusMercatoris
   venv/bin/python -m pip install -r AI/requirements.txt
   ```

3. Restart the MVC application so `PythonRunnerService` can restart the AI process with the updated environment.


8. AI Flow Diagram (Mermaid)
----------------------------

The diagram below shows how the ASP.NET MVC application, the Python AI service, and the database interact.

```mermaid
flowchart LR

%% Startup
A[ASP.NET MVC App] --> B[PythonRunnerService]
B --> C[Start AI/api.py via Uvicorn]
C --> D[FastAPI AI Service<br/>(port 5001)]

%% Feature extraction
E[Admin / Moderator UI] --> F[Upload / Edit Product]
F --> G[ASP.NET MVC Controllers]
G --> H[ClusteringService.ExtractAndStoreFeaturesAsync]

H --> I[Read product images from wwwroot]
I --> J[HTTP POST /extract<br/>multipart/form-data: files[]]
J --> D

D --> K[ResNet-based feature extractor]
K --> L[2048-dim feature vector]

L --> M[Return JSON: { vector: [...] }]
M --> N[Save features in ProductFeatures]
N --> O[(Database)]

%% Clustering
P[Admin triggers clustering<br/>from MVC UI] --> Q[ClusteringService.RunClusteringAsync]

Q --> R[Load feature vectors<br/>from ProductFeatures]
R --> S[Build feature matrix<br/>(features: List&lt;List&lt;float&gt;&gt;)]

S --> T[HTTP POST /cluster<br/>JSON: { features, k }]
T --> D

D --> U[Run K-Means (sklearn)]
U --> V[Return JSON:<br/>{ labels: [...], centroids: [...] }]

V --> W[Update ProductClusters<br/>and ProductClusterMembers]
W --> O

O --> X[UIs read clustered products]
```
