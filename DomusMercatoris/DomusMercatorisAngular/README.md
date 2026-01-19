# Solidus (DomusMercatoris Client)

Modern, performance-oriented Angular e-commerce frontend application with a brutalist design.

![Angular](https://img.shields.io/badge/Angular-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-007ACC?style=for-the-badge&logo=typescript&logoColor=white)
![Brutalist Design](https://img.shields.io/badge/Design-Brutalist-black?style=for-the-badge)

## 🚀 About The Project

Solidus is the Angular-based client application for the **DomusMercatoris** project. It delivers a unique user experience by adopting "Brutalist" design principles (thick borders, monospace fonts, sharp corners). It is developed using Angular's latest features (Standalone Components, Signals, Control Flow Syntax).

## ✨ Key Features

### 🎨 Brutalist UI Design
- **Bold and Clear:** Thick black borders, high contrast, and monospaced typography.
- **Minimalist Animations:** Functional transitions stripped of unnecessary decorations.

### 🔍 Advanced Search Experience
- **Expandable Search Bar:** Search area that opens with a stylish animation on the navbar.
- **Visual Search:**
  - **Drag & Drop:** Users can drag and drop product photos onto the search bar to find similar products.
  - **Format Support:** Automatic conversion and processing of HEIC format images from Apple devices via `heic2any` integration.
  - **Camera Integration:** Upload images directly from the device camera or file selector.

### ⚡ Performance and UX
- **Infinite Scroll:** Natural flow product listing developed using the `IntersectionObserver` API, eliminating inner scroll issues.
- **Responsive Structure:** Mobile-first design approach. Dynamic column structure (1-4 columns based on screen width).

### 🛠 Technical Infrastructure
- **Angular 17+ (Standalone Components):** Module-free, modern architecture.
- **Signals:** Angular Signals for reactive state management.
- **Proxy Configuration:** Seamless development environment with Backend API.

## 📦 Installation and Running

Follow these steps to run the project locally:

### Prerequisites
- Node.js (LTS version recommended)
- npm

### Steps

1. **Clone the repository:**
   ```bash
   git clone <repo-url>
   cd DomusMercatorisAngular
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Start the development server:**
   ```bash
   npm start
   ```
   This command starts the application. Check the terminal output for the correct local URL (usually `http://localhost:4200`, but Angular may choose a different port if 4200 is in use). API forwarding is handled via `proxy.conf.json`.

## 🧪 Tests

The application uses the **Vitest** test runner.

- **To Run Unit Tests:**
  ```bash
  npm test
  ```

## 📂 Project Structure

```
src/app/
├── components/       # Reusable UI components (Header, Footer, SearchBar, ProductList, etc.)
├── guards/           # Route guards (AuthGuard, etc.)
├── interceptors/     # HTTP request/response interceptors (Token, Error handling)
├── models/           # TypeScript interfaces and data models
├── pages/            # Page components (Home, Search, Profile, etc.)
├── services/         # Business logic and API services (ProductService, CartService, etc.)
└── app.routes.ts     # Application routing configuration
```

---
© 2026 Solidus. All rights reserved.
