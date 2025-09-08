# MagiDesk (Samsung Magician Pro–style)

A minimal, production-ready solution with:

- Frontend: WinUI 3 (.NET 8), MVVM pattern, Samsung Magician Pro–style navigation and toolbar
- Backend: ASP.NET Core 8 Web API
- Database: Google Firestore (with credentials from `backend/config/serviceAccount.json` or `GOOGLE_APPLICATION_CREDENTIALS`)
- Shared: DTOs for API <-> UI

## Solution Layout

```
solution/
  backend/
    MagiDesk.Backend/           # ASP.NET Core Web API
      Controllers/
      Models/
      Services/
      Dockerfile
      appsettings.json
    config/
      serviceAccount.json       # Firestore credentials (placeholder)
  frontend/
    MagiDesk.Frontend.csproj    # WinUI 3 app
    Views/                      # Pages, dialogs, and shell
    ViewModels/
    Services/                   # ApiClient
    appsettings.json            # Backend base URL
  shared/
    MagiDesk.Shared.csproj      # DTO library
    DTOs/
  docs/
    README.md
```

## Backend

### Configure Firestore

Set your Google Cloud project ID and credentials path in `backend/MagiDesk.Backend/appsettings.json`:

```json
{
  "Firestore": {
    "ProjectId": "YOUR_GCP_PROJECT_ID",
    "CredentialsPath": "config/serviceAccount.json"
  }
}
```

Optionally, set `GOOGLE_APPLICATION_CREDENTIALS` to override the path.

Place your service account JSON at `backend/config/serviceAccount.json` (do not commit real credentials to source control).

### Run Locally

From `solution/` run:

```powershell
# restore/build
 dotnet build MagiDesk.sln -c Debug

# run backend (HTTPS enabled by default)
 dotnet run --project backend/MagiDesk.Backend/MagiDesk.Backend.csproj --launch-profile https
```

The API is available at `https://localhost:5001` (Swagger UI available in Development).

### API Endpoints

- For a complete list of routes across all controllers (Vendors, Items, Orders, Inventory, Jobs, Cash Flow, Users/Auth, Settings), see [api-routes.md](./api-routes.md).

## Deploy Backend to Cloud Run

1. Build and push the container (adjust `PROJECT_ID` and image name):

```bash
gcloud auth configure-docker
gcloud config set project YOUR_GCP_PROJECT_ID

# From solution/backend/MagiDesk.Backend
docker build -t gcr.io/YOUR_GCP_PROJECT_ID/magidesk-backend:latest .
docker push gcr.io/YOUR_GCP_PROJECT_ID/magidesk-backend:latest
```

2. Create a Secret Manager secret for the service account JSON (recommended):

```bash
echo "<your-service-account-json>" | gcloud secrets create firestore-sa --data-file=- --replication-policy=automatic
```

3. Deploy to Cloud Run with the secret mounted as a file and set environment variables:

```bash
gcloud run deploy magidesk-backend \
  --image gcr.io/YOUR_GCP_PROJECT_ID/magidesk-backend:latest \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars Firestore__ProjectId=YOUR_GCP_PROJECT_ID \
  --set-env-vars GOOGLE_APPLICATION_CREDENTIALS=/secrets/serviceAccount.json \
  --update-secrets /secrets/serviceAccount.json=firestore-sa:latest
```

4. Copy the deployed URL (e.g., `https://magidesk-backend-xxxx-uc.a.run.app`).

## Frontend (WinUI 3)

The WinUI 3 app is set up with:
- NavigationView shell
- Toolbar buttons (Add/Edit/Delete/Refresh)
- Dark/Light mode toggle
- Vendors/Items pages with context menus and dialogs

Set the backend URL in `frontend/appsettings.json`:

```json
{
  "Backend": {
    "BaseUrl": "https://localhost:5001"
  }
}
```

After you deploy the backend, replace the URL with your Cloud Run URL.

### Run Frontend

Open the solution in Visual Studio 2022 (17.10+) with Windows App SDK installed, then set `MagiDesk.Frontend` as startup and run.

Or via CLI from `solution/`:

```powershell
 dotnet build frontend/MagiDesk.Frontend.csproj -c Debug
 # To run, use Visual Studio or `winappdbg`-compatible runner (recommended to use VS for WinUI 3 apps)
```

Note: If you see a XAML compile error, ensure the Windows App SDK (1.4+) and Windows 10 SDK are installed. This repo uses Microsoft.WindowsAppSDK 1.7.x. You may need to install `Windows 10 SDK (10.0.19041.0+)` via Visual Studio Installer.

## Notes on Firestore

`FirestoreService` uses channel credentials from the service account JSON and binds options from configuration. You can also set `GOOGLE_APPLICATION_CREDENTIALS` to point directly to the JSON file.

## Next Steps

- Re-enable all frontend pages if temporarily excluded for XAML checks.
- Polish styles to more closely match Samsung Magician Pro.
- Add unit tests and logging improvements.

## Security

Do not commit real credentials. Use environment variables and Google Secret Manager for production.

## Documentation Index

- Feature Log: [feature-log.md](./feature-log.md)
- API Routes: [api-routes.md](./api-routes.md)
- API Contract: [api-contract.md](./api-contract.md)
- Modules:
  - Backend: [modules/backend.md](./modules/backend.md)
  - Frontend: [modules/frontend.md](./modules/frontend.md)
  - Shared DTOs: [modules/shared.md](./modules/shared.md)
  - Settings API: [modules/settings-api.md](./modules/settings-api.md)
