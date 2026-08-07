# Specimen Check-In & Reconciliation System (Multi-Tenant)

This repository contains a complete multi-tenant Pathology Lab Specimen Check-In and Reconciliation solution. It consists of an **ASP.NET Core 8 Web API** backend with **Entity Framework Core** and a **SQL Server** database, coupled with a modern **Angular** standalone web frontend.

---

## 🚀 How to Run Locally

Follow these steps to run the application in your local development environment:

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (latest LTS)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for local database execution)

---

### Step 1: Run SQL Server (via Docker)
Start the SQL Server database container:
```bash
# In the repository root
docker-compose up -d
```
*Note: Make sure to copy `.env.example` to `.env` and configure your default system parameters if needed.*

---

### Step 2: Apply Database Migrations & Run Backend
1. **Apply Migrations:** Apply EF Core migrations to configure the SQL Server database schema:
   ```bash
   dotnet ef database update --project SpecimenCheckIn.Api --startup-project SpecimenCheckIn.Api
   ```
2. **Run Backend API:** Run the ASP.NET Core API server:
   ```bash
   dotnet run --project SpecimenCheckIn.Api
   ```
   The API will start and expose:
   - **Swagger UI:** `http://localhost:5000` (opens automatically in local runs, redirects to root)

---

### Step 3: Run Angular Web App
1. **Install Dependencies:** Navigate to the Angular workspace and install npm packages:
   ```bash
   cd specimen-checkin-web
   npm install
   ```
2. **Serve Application:** Run the local dev server:
   ```bash
   npx ng serve
   ```
   Open your browser and navigate to `http://localhost:4200` to interact with the application.

---

## 🛠 Technology Stack & Database Choice

- **Backend Web API:** Built with **ASP.NET Core 8 Web API** providing standard, RESTful controllers, Swagger documentation, and CORS configuration.
- **ORM / Database:** Utilizes **Entity Framework Core** mapped with **SQL Server** (local development container runs `mcr.microsoft.com/mssql/server:2022-latest` with a persisted volume).
- **Testing:** Configured with an **xUnit** test project (`SpecimenCheckIn.Tests`) containing tests utilizing EF Core's InMemory provider for rapid validation of query filters and controller behaviors.
- **Frontend UI:** Built with the latest **Angular** (standalone components, no NgModules) featuring reactive forms validation, typed Angular services, functional interceptors, and a custom CSS glassmorphism theme.

---

## 📝 Assumptions & Core Domain Boundaries

1. **Multi-Tenant Boundary:** In this system, the multi-tenant boundary is defined by a **`Lab` (Laboratory)**. All manifests, specimens, and discrepancies are owned directly or indirectly by a specific `Lab`.
2. **Authentication Mocking:** Instead of a complex, stateful auth flow (like OAuth or JWT), a header-based tenant selector stands in for authenticating users. Users can switch their context in the UI header dropdown, which automatically sets the scoped `X-Lab-Id` request header injected on all outgoing API calls.
3. **Validations:** Request DTO models include strict `DataAnnotations` mirroring frontend validation constraints. If invalid data bypasses the client-side validation, the backend returns standardized `ProblemDetails` error shapes.

---

## 🏛 Architecture & Design Patterns

### 1. Robust Tenant Isolation
- **Context Service:** The scoped `CurrentLabContext` service extracts the `X-Lab-Id` GUID from HTTP request headers.
- **Enforced DbContext Isolation:** To prevent queries from accidentally exposing cross-tenant data, EF Core **Global Query Filters** are applied at the database context level:
  - `Manifest` is filtered by the active context: `m => m.LabId == activeLabId`
  - `Specimen` and `Discrepancy` query filters cascade access through the navigation property:
    - `s => s.Manifest.LabId == activeLabId`
    - `d => d.Manifest.LabId == activeLabId`
- **Audit Interceptor:** Overridden `SaveChanges` / `SaveChangesAsync` in `ApplicationDbContext` automatically assign the current `LabId` to newly created `Manifest` records.

### 2. Dual-Layer Validation & Typed Integrations
- **Mirrored Data Structures:** Angular interfaces (`/src/app/models`) mirror C# entities, ensuring strong typing across network layers.
- **Validation Symmetry:** Validation rules (e.g. required reason text lengths) are duplicated client-side (Angular Reactive Forms) for immediate user feedback and server-side (DataAnnotations DTOs) to guarantee data integrity.
- **Shared State Management:** Angular signals inside `ManifestService` manage the selected manifest and specimens. The left panel list and right detail board share this signal state reactively, eliminating complex parent-child event bubbles.

---

## 🔮 With More Time, I Would...

- **Add Audit Logging:** Implement database-level audit logs tracking exactly which operator checks in, flags, or resolves discrepancies on specimens.
- **Support Off-Manifest Specimens:** Extend the discrepancy modal to register completely off-manifest specimen barcodes, raising `OffManifest` type discrepancies with null specimen IDs.
- **Real Authentication & RBAC:** Implement authentication (e.g. MS Entra or OpenID Connect) and Role-Based Access Control to separate standard *Lab Tech* receptionists from *Lab Managers* authorized to force-close manifests.
- **End-to-End Testing:** Implement end-to-end tests using Cypress or Playwright to simulate specimen scanning and reconciliation.
- **Azure Deployment Pipeline:** Configure GitHub Actions workflows to deploy the containerized SQL Server and API to Azure Container Apps, with Angular served via Azure Static Web Apps.
