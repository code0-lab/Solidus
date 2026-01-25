# Domus Mercatoris - Backend API (.NET 8)

This is the core REST API for the Domus Mercatoris platform, built with **ASP.NET Core 8**. It handles business logic, database interactions, authentication, and orchestrates communication with the Python AI service.

## 🛠 Technology Stack

*   **Framework:** .NET 8 (ASP.NET Core Web API)
*   **Database:** Entity Framework Core (SQL Server / PostgreSQL compatible)
*   **Authentication:** JWT (JSON Web Tokens)
*   **Documentation:** Swagger / OpenAPI

## ✨ Key Features

*   **Product Management:** CRUD operations for products, categories, brands, and banners.
*   **User Management:** Authentication, registration, and role-based access control (Admin, Moderator, User).
*   **AI Integration Proxy:**
    *   The `ClusteringController` acts as a gateway for visual search.
    *   Receives images from the frontend.
    *   Forwards them to the Python AI Service (`/extract` endpoint).
    *   Handles fallback mechanisms if the Python service is unavailable or if client-side processing fails.
*   **Sales & Orders:** Management of sales, cargo tracking, and customer orders.

## 🚀 Getting Started

### Prerequisites

*   .NET 8 SDK
*   A running database instance (connection string configured in `appsettings.json`)
*   The Python AI Service (for visual search features)

### Configuration

Ensure `appsettings.json` is configured correctly:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=DomusMercatoris;..."
  },
  "AiService": {
    "Url": "http://localhost:5001"
  }
}
```

### Running the API

1.  Navigate to the project directory:
    ```bash
    cd DomusMercatorisDotnetRest
    ```
2.  Restore dependencies:
    ```bash
    dotnet restore
    ```
3.  Run the application:
    ```bash
    dotnet run
    ```
    The API will start (default: `http://localhost:5200`).

## 🖼 Image Processing Fallback

While the primary image processing workflow involves client-side cropping (Angular) and server-side background removal (Python), this API includes a **fallback mechanism** using `SixLabors.ImageSharp`.

If an image bypasses the standard flow, the API can:
1.  Resize the image to **224x224**.
2.  Apply a **white background** (handling transparency).
3.  Convert to **JPEG** before sending to the AI service.

## 📂 Project Structure

*   `Controllers/`: API Endpoints.
*   `Services/`: Business logic layer.
*   `Infrastructure/`: Cross-cutting concerns (Middleware, Helpers).
*   `Migrations/`: EF Core database migrations.
