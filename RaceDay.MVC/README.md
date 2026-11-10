# RaceDay Part 3 — ASP.NET Core MVC

The MVC front end uses the existing ASP.NET Core API for **all data operations**. It does not reference EF Core or connect to SQL Server directly. Each user gets their own MVC session, which securely holds their API session cookie.

## Visual Studio quick start

1. Open **RaceDay.sln** in Visual Studio 2022 with .NET 8 installed.
2. Configure a working `ConnectionStrings:DefaultConnection` for **RaceDay.Api** (the existing SQL Server / RaceDayDb). Do not commit database passwords.
3. Right-click the solution → **Configure Startup Projects** → **Multiple startup projects**:
   - RaceDay.Api: **Start**
   - RaceDay.MVC: **Start**
   - RaceDay.Contracts / tests: **None**
4. Rebuild the solution, then press **F5**.
5. Test the API JSON health endpoint at **https://localhost:59670/api/health** and Swagger at **https://localhost:59670/swagger**. The actual port can be different if you changed the API launch profile.
6. Open the MVC website at **http://localhost:5050**.

The default MVC API destination is `https://localhost:59670/`, matching the API's development launch profile. Override `RaceDayApi:BaseUrl` via environment configuration if your API port changes. Do **not** point it at the MVC port. An HTTP-to-HTTPS redirect is treated as an error because server-side API calls deliberately do not follow redirects.

To trust the local development HTTPS certificate:
`dotnet dev-certs https --trust`

## Database migrations (important for login)

Part 3 added `ProfileImageUrl` to Users and `BannerImageUrl` to Events. If your existing database lacks these columns, registration might detect duplicate emails while login fails when the API loads a full user record.

For local **Development** API launches, `RaceDay.Api/appsettings.Development.json` enables `RaceDay:AutoMigrate`, so EF Core applies pending migrations when the API starts. Automatic migrations are opt-in outside this environment. If you use a pre-existing database created entirely by a handwritten SQL script rather than EF migrations, reconcile the EF migration history before enabling automatic migrations.

Manual alternative in a terminal:
`dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api`

Or in Visual Studio's Package Manager Console:
`Update-Database -Project RaceDay.Api -StartupProject RaceDay.Api`

If the API cannot start or SQL Server reports an invalid column, inspect the API Output window and verify the configured connection string and database migrations. Never share SQL passwords in issue reports.

## Features

**Participants:** register/log in, search events by name/location/type, browse categories/routes, enrol in a category, cancel an enrolment, view status/payment field, view published finish times and positions, update profile and photo.

**Organisers:** register/log in, dashboard with event and entrant totals, create/edit/delete events and upload event banners, create/edit/delete categories, create/edit/delete routes, view and change enrolment status, capture and correct results.

**Public:** searchable event catalogue, event details and public leaderboard.

**Current data limitations:** payment status is recorded but there is no integrated payment gateway. Published finisher totals in My Results count available "Finished" result records, not all event entrants. The existing Part 2 API allows self-selection of Organiser accounts, which is suitable for coursework but not production role governance.

## Azure Blob Storage and Azurite (Part 3 notes, pages 40–50)

The API owns the Azure Storage connection and uploads; MVC never directly accesses Blob Storage. Private containers: `banners` and `profiles`. JPG/PNG/WebP up to 5 MB, with basic file-signature checks.

During local Development, `RaceDay.Api/appsettings.Development.json` sets `AzureStorage:ConnectionString` to `UseDevelopmentStorage=true`. Start Azurite on `127.0.0.1:10000` first (via Visual Studio Connected Services, npm, or `docker run --rm -p 127.0.0.1:10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0`). The Blob SDK writes images to private `banners` and `profiles` containers; SQL Server stores the paths. Verify both uploading and display by refreshing MVC, and inspect the containers in Azure Storage Explorer.

For cloud Azure Storage instead, configure the API secret (replace the placeholder locally, never commit it):

`dotnet user-secrets set "AzureStorage:ConnectionString" "<your-actual-storage-connection-string>" --project RaceDay.Api`

If Azurite is not running or real Azure Storage is unavailable, core race features still work. Image operations return HTTP 503 with a useful error instead of a raw SDK exception.

## Docker Compose

Copy `.env.example` to `.env` and set a strong SQL password. The Docker stack now includes Azurite with a persistent blob volume and automatically connects API to `http://azurite:10000`. Leave the Azure Storage variable empty for Azurite, or set a real Azure cloud connection string if required. Run:

`docker compose up --build -d`

Open MVC at http://localhost:8080, the API health check at http://localhost:8081/api/health, and Azurite at http://127.0.0.1:10000. The API service uses Docker-internal `http://api:8080/`. Compose enables automatic EF Core migrations on the SQL container. Production deployment should use a controlled migration process, persistent session store and Data Protection keys.

## Debugging MVC/API requests

`RaceDay.MVC/Services/RaceDayApiClient.cs` logs the request method, route and API status code without logging passwords or session cookies. Failed JSON/redirect responses show HTTP status and a short preview rather than "The API returned an unexpected response."

For example, **405 Method Not Allowed** usually means the wrong API server or endpoint; **307 Redirect** indicates an HTTP/HTTPS mismatch; **500** may indicate a missing migration or unavailable database; **401** indicates credentials or an expired session. Confirm `/api/health` and inspect the API Output window.

## Automated checks

The GitHub Actions workflow restores/builds the solution and runs both API and MVC test projects. The MVC tests verify API redirects, JSON errors, per-visitor cookie forwarding, and a full MVC login-form → organiser-dashboard flow with an in-memory API simulation. They do not replace a browser test against your own SQL Server and Azure Storage account.

## Lecturer's additional Part 3 class notes

Read [the RaceDay-specific adaptation and full acceptance checklist](../docs/PROG6212_PART3_ADAPTED_GUIDE.md). The slides use a different Course Registration system with JWT/bearer tokens and Admin/Lecturer/Student. RaceDay intentionally retains its Part 2 session-cookie authentication with Organiser/Participant roles; do not paste JWT-specific sample code into it.
