# Domus Mercatoris

Domus Mercatoris is a modern, microservices-based e-commerce platform built with a "Brutalist" design philosophy. It integrates advanced AI capabilities for visual product search and features a robust, scalable architecture.

## 🏗 System Architecture

The project consists of three main components:

1.  **Frontend (Angular):**
    *   **Project:** `DomusMercatorisAngular`
    *   **Tech Stack:** Angular 17+ (Standalone Components, Signals), TypeScript.
    *   **Features:** Brutalist UI, Infinite Scroll, **Visual Search with Image Cropping**.
    *   **Responsibility:** User interface, handling user input, cropping images for AI processing.

2.  **Backend API (ASP.NET Core):**
    *   **Project:** `DomusMercatorisDotnetRest`
    *   **Tech Stack:** .NET 9, C#, Entity Framework Core.
    *   **Features:** REST API, JWT Authentication, Product Management, **AI Proxy/Gateway**.
    *   **Responsibility:** Business logic, database operations, orchestrating requests between Frontend and AI Service.

3.  **AI Service (Python):**
    *   **Project:** `AI`
    *   **Tech Stack:** Python 3.11, FastAPI, PyTorch (ResNet-50), **Rembg**, **Pillow-Heif**.
    *   **Features:** Image Feature Extraction, Background Removal, Clustering, **HEIC Support**.
    *   **Responsibility:** Processing images to generate feature vectors for visual search.

## 🏛 System Architecture (High Level)

```mermaid
graph TD
    User[User / Client]
    
    subgraph Frontend
        Angular[Angular SPA]
        MVC[MVC Admin Panel]
    end
    
    subgraph Backend
        API["ASP.NET Core API (.NET 9)"]
        DB[(SQL Server)]
    end
    
    subgraph AI Infrastructure
        AIService[Python AI Service]
        ResNet[ResNet-50 Model]
    end
    
    User -->|Uses| Angular
    User -->|Manages| MVC
    
    Angular -->|REST / JWT| API
    MVC -->|Direct Access| DB
    MVC -->|Manages| AIService
    
    API -->|EF Core| DB
    API -->|HTTP| AIService
    
    AIService -->|Inference| ResNet
```

## 🖼 The "Golden Ratio" Image Processing Pipeline

To ensure the highest accuracy for visual search (ResNet-50), we implement a specific processing pipeline:

```mermaid
graph LR
    A[User Upload] -->|Raw Image| B[Angular Cropper]
    B -->|Cropped Image| C[Backend API]
    C -->|Forward| D[AI Service]
    subgraph Python AI Service
    D -->|Rembg| E[Remove Background]
    E -->|Transparent| F[Smart Preprocessing]
    F -->|Resize/Pad to 224x224| G[ResNet-50]
    G -->|Extract| H[Feature Vector]
    end
    H -->|JSON| C
```

1.  **User Selection (Angular):** The user uploads an image and **crops** the relevant area using the client-side cropper (`ngx-image-cropper`). This ensures only the object of interest is sent.
2.  **Background Removal (Python):** The cropped image is sent to the AI service, where **`rembg`** removes the background, isolating the product.
3.  **Smart Preprocessing (Python):** The transparent image is composited over a **white background** and resized to **224x224** while preserving the aspect ratio (padding with white).
4.  **Feature Extraction (Python):** The processed image is fed into ResNet-50 to generate a feature vector.

## 🚀 Getting Started

### Prerequisites

*   **Node.js** (LTS) & **npm**
*   **.NET 9 SDK**
*   **Python 3.11** (Required for `rembg` compatibility)
*   **Docker** (Optional, for containerized deployment)

### Running the Project

1.  **AI Service:**
    *   Navigate to the root directory.
    *   Set up the virtual environment: `python3.11 -m venv venv`
    *   Activate and install requirements: `source venv/bin/activate && pip install -r AI/requirements.txt`
    *   The .NET application will attempt to start this service automatically, or you can run it manually.

2.  **Backend API:**
    *   Navigate to `DomusMercatorisDotnetRest`.
    *   Run `dotnet run`.
    *   The API usually listens on `http://localhost:5200`.

3.  **Frontend:**
    *   Navigate to `DomusMercatorisAngular`.
    *   Run `npm install` (first time).
    *   Run `npm start`.
    *   Access the app at `http://localhost:4200`.

4.  **Database Migration:**
    If setting up for the first time, run migrations to create the database:
    ```bash
    dotnet ef database update --project DomusMercatoris.Data --startup-project DomusMercatorisDotnetRest
    ```

4.  **Admin & Moderator Panel (MVC):**
    *   Navigate to `MVC/MVC`.
    *   Run `dotnet run`.
    *   Access the panel at `http://localhost:5000` (or the port specified in the output, e.g., 5147).

### 🔐 Default Credentials (Database Seeder)

The application automatically seeds the following accounts for testing/development:

| Role | Email | Password | Access |
| :--- | :--- | :--- | :--- |
| **Rex** (Super Admin) | `rex@domus.com` | `RexPassword1!` | Full System Access |
| **Moderator** | `moderator@domus.com` | `ModeratorPassword1!` | Moderator Panel, Clustering |
| **Manager** | `manager@domus.com` | `ManagerPassword1!` | Company Management |
| **User** | `test@domus.com` | `TestUserPassword1!` | Customer Features |

> **Note:** The "Domus" link on the Login page navigates to the special Moderator login area.

## 📡 API Endpoints (Summary)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| **AUTH** | | |
| `POST` | `/api/users/login` | User login |
| `POST` | `/api/users/register` | User registration |
| **PRODUCTS** | | |
| `GET` | `/api/products` | Paged list of products |
| `GET` | `/api/products/{id}` | Product details |
| `POST` | `/api/clustering/classify` | AI Visual Search |
| **COMPANIES** | | |
| `GET` | `/api/companies` | List all companies |
| **USER** | | |
| `GET` | `/api/users/me` | Current user profile |

## 📸 Screenshots

### MVC Admin Panel
> *Place your screenshots here (e.g. Dashboard, Product Edit)*

### Angular Frontend
> *Place your screenshots here (e.g. Home, Visual Search Modal)*

## 📚 Postman Collection / OpenAPI

A full OpenAPI (Swagger) definition is available, which contains **all** API endpoints. You can import this file directly into Postman.

*   [Download OpenAPI Definition](./DomusMercatoris.openapi.json)
*   *Alternatively, import URL in Postman:* `http://localhost:5280/swagger/v1/swagger.json`

## 📂 Repository Structure

*   `AI/` - Python FastAPI service for AI operations.
*   `DomusMercatorisAngular/` - Angular Frontend application.
*   `DomusMercatorisDotnetRest/` - Main ASP.NET Core REST API.
*   `DomusMercatoris.Core/` - Shared Domain Entities and Interfaces.
*   `DomusMercatoris.Data/` - Database Context and Infrastructure.
*   `DomusMercatoris.Service/` - Business Logic Services.

---
© 2026 Solidus. All rights reserved.
