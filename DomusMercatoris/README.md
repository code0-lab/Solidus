Domus Mercatoris – Solution Overview and Setup
==============================================

This repository contains the full Domus Mercatoris system:

- **DomusMercatorisDotnetRest** – .NET REST API (backend)
- **MVC** – ASP.NET Core MVC / Razor Pages application (admin and web UI)
- **DomusMercatorisAngular** – Angular SPA (shop frontend)
- **AI** – Python-based AI service used for image feature extraction and clustering

This document explains how to set up and run each part locally.


Prerequisites
-------------

- .NET SDK 9.0 or compatible
- Node.js and npm
- Python 3 (available as `python3` on your PATH)
- macOS or Linux (commands below use Unix-style paths)


1. Restore and Build .NET Projects
----------------------------------

From the solution root:

```bash
cd /path/to/DomusMercatoris
dotnet restore DomusMercatoris.sln
dotnet build DomusMercatoris.sln
```

This builds:

- `DomusMercatoris.Core`
- `DomusMercatoris.Data`
- `DomusMercatoris.Service`
- `DomusMercatorisDotnetRest`
- `MVC` (DomusMercatorisDotnetMVC)


2. Run the REST API
-------------------

From the solution root:

```bash
cd /path/to/DomusMercatoris
dotnet run --project DomusMercatorisDotnetRest/DomusMercatorisDotnetRest.csproj
```

This starts the REST API, serving JSON endpoints used by the frontend.


3. Run the MVC Application
--------------------------

From the solution root:

```bash
cd /path/to/DomusMercatoris
dotnet run --project MVC/MVC/MVC.csproj
```

The MVC app hosts the admin UI and is also responsible for starting the Python AI service via `PythonRunnerService`.


4. Set Up and Run the Angular Frontend
--------------------------------------

From the Angular project directory:

```bash
cd /path/to/DomusMercatoris/DomusMercatorisAngular
npm install
npm run start
```

The Angular app:

- Uses `ng serve` with `proxy.conf.json` for API calls.
- Exposes the shop frontend (see `angular.json` and `proxy.conf.json` for details).


5. Set Up the Python AI Service
-------------------------------

The Python AI service lives in the `AI` folder and is started automatically by the MVC app. It uses FastAPI and Uvicorn.

From the solution root:

```bash
cd /path/to/DomusMercatoris
python3 -m venv venv
venv/bin/python -m pip install --upgrade pip
venv/bin/python -m pip install -r AI/requirements.txt
```

This creates a virtual environment at the solution root and installs all dependencies required by `AI/api.py` (FastAPI, Torch, scikit-learn, etc.).

For more detailed information about the AI service (endpoints, troubleshooting), see:

- `AI/README.md`


6. How the MVC App Integrates with the AI Service
-------------------------------------------------

The MVC project includes `PythonRunnerService`:

- File: `MVC/MVC/Services/PythonRunnerService.cs`
- Features:
  - **Cross-Platform**: Automatically selects the correct Python executable for Windows (`venv/Scripts/python.exe`) or Linux/macOS (`venv/bin/python`).
  - **Resilience**: Automatically restarts the Python service if it crashes.
  - **Cleanup**: Kills zombie processes on startup and ensures clean shutdown on exit.
  - **Logging**: Pipes Python stdout/stderr into ASP.NET Core logs for easier debugging.

The clustering logic in the MVC app (for example in `ClusteringService`) calls the Python API endpoints at `http://localhost:5001` to:

- Extract image feature vectors.
- Run K-Means clustering.


7. Running the Python AI Service Manually (Optional)
----------------------------------------------------

For debugging, you can start the Python AI service manually:

```bash
cd /path/to/DomusMercatoris
source venv/bin/activate
python AI/api.py
```

This will start a FastAPI server via Uvicorn on:

- `http://0.0.0.0:5001`

You can then hit its endpoints (for example with `curl` or Postman) to verify behavior independently of the MVC app.


8. Common Problems
------------------

**Problem: MVC logs show Python import errors (e.g. `ModuleNotFoundError: No module named 'fastapi'`)**

- Ensure the virtual environment exists at the solution root.
- Ensure dependencies are installed:

  ```bash
  cd /path/to/DomusMercatoris
  venv/bin/python -m pip install -r AI/requirements.txt
  ```

**Problem: MVC logs show that the Python service will not start or cannot find the `AI` folder**

- Make sure:
  - The `AI` folder is present at the solution root.
  - You are running the MVC app from within this repository and not from a different working directory.


9. Where to Look in the Code
----------------------------

- AI service implementation:
  - `AI/api.py`
- AI service CLI utilities:
  - `AI/main.py`
- MVC Python integration:
  - `MVC/MVC/Services/PythonRunnerService.cs`
  - `MVC/MVC/Services/ClusteringService.cs`
- REST API:
  - `DomusMercatorisDotnetRest/Program.cs`
- Angular frontend:
  - `DomusMercatorisAngular/src/app`

