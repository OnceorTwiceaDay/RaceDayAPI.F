# RaceDay Event Management System — Part 3 (ASP.NET Core MVC)

**Module:** PROG6212 — Programming 2B  
**Submission:** Part 3 — ASP.NET Core MVC Front End, Azure Blob Storage and Docker  
**Repository:** https://github.com/OnceorTwiceaDay/RaceDayAPI.F  
**Technology:** .NET 8 / C# / ASP.NET Core MVC / ASP.NET Core Web API / SQL Server

## 1. Project overview

RaceDay is a South African sporting-event management system for **running, walking and cycling**. This Part 3 implementation adds an ASP.NET Core MVC website to the Part 2 REST API. Organisers manage events, categories, routes, enrolments and results; participants discover events, enrol and view their performance.

**Architectural requirement:** The MVC project **does not connect directly to SQL Server and does not contain a `DbContext`**. All persistent data operations go through the existing RaceDay API using `RaceDayApiClient`. The API owns database access, validation, authentication/authorisation and Blob Storage operations.

~~~text
Browser (Organiser / Participant)
               |
               v
RaceDay.MVC (Razor views, controllers, MVC session)
               |
               | HTTP + per-user API session cookie
               v
RaceDay.Api (REST endpoints, role/ownership checks)
               |                           |
               v                           v
EF Core 8 -> SQL Server            Azure Blob Storage
                                    (Azurite locally)
~~~

The API and MVC preserve **session-cookie authentication** from Part 2. The lecturer's sample Course Registration project uses JWT and different roles; it is a teaching example, not the authentication scheme implemented by RaceDay.

## 2. Technologies

| Component | Implementation |
| --- | --- |
| User interface | ASP.NET Core MVC 8, Razor views, Bootstrap, CSS |
| Backend | ASP.NET Core Web API 8 and REST/JSON |
| Communication | Typed `HttpClient` through `RaceDayApiClient` |
| Authentication | API `RaceDay.Session` cookie forwarded per MVC visitor; MVC `RaceDay.Mvc.Session` |
| Access control | Organiser/Participant role filters and API ownership checks |
| Data | Entity Framework Core 8 (API only), SQL Server |
| Images | Azure Storage Blobs SDK; local Azurite emulator |
| Containers | Dockerfiles for MVC and API; Docker Compose for MVC, API, SQL Server, Azurite |
| Automated tests | xUnit API and MVC test projects |
| CI | GitHub Actions: build, Docker image build, Azurite-backed tests |

## 3. Repository and solution structure

~~~text
RaceDay.sln
README.md
.env.example
docker-compose.yml
.github/workflows/raceday-ci.yml
docs/
  PROG6212_PART3_ADAPTED_GUIDE.md
  RaceDay_ExistingDb_Part3_Repair.sql
RaceDay.Api/
  Controllers/   Data/   Models/   Filters/   Services/
  Migrations/    Program.cs   Dockerfile
RaceDay.Contracts/
  Dtos.cs
RaceDay.MVC/
  Controllers/   Models/   Services/   Views/   wwwroot/
  Program.cs     Dockerfile    README.md
RaceDay.Api.Tests/
RaceDay.MVC.Tests/
~~~

- **`RaceDay.Api`:** owns authentication, event management, database entities, result processing, and image storage.
- **`RaceDay.MVC`:** provides pages and forms, calls the API, maintains visitor sessions and displays returned results.
- **`RaceDay.Contracts`:** shared request and response data-transfer objects (DTOs).
- **Test projects:** API tests, MVC client tests and integrated login/storage checks.
- **`docs`:** planning notes and the non-destructive repair script for legacy SQL databases.

## 4. Implemented functionality

### Public visitor

- View and search available events by name, location or event type.
- Open event details with categories, routes, banners and available results.
- Register or sign in as an Organiser or Participant.

### Organiser

- Log in to a role-specific dashboard showing owned events and enrolment counts.
- Create, view, edit and delete **owned events**.
- Add, update and delete event **categories** and **routes**.
- See participants enrolled in an event and update enrolment statuses.
- Record or correct finishing results, including times, positions and statuses.
- Edit their profile and upload a profile image.
- Upload an event banner (JPG, PNG or WebP, up to 5 MB).

### Participant

- Browse event details and select a category for enrolment.
- View **My Enrolments**, enrolment status and payment-status field.
- Cancel an eligible enrolment.
- View **My Results**, including recorded finishing information.
- Edit their profile and upload a profile image.

**Scope clarification:** The system records `PaymentStatus`, but does **not** integrate a payment gateway or process real transactions.

## 5. Run in Visual Studio 2022 (local development)

### Prerequisites

- Visual Studio 2022 with the ASP.NET and web-development workload.
- .NET 8 SDK.
- SQL Server/SQL Server Express and, optionally, SQL Server Management Studio (SSMS).
- Azurite for local image upload tests (see Section 7).
- A copy of this repository.

### Setup

1. Open `RaceDay.sln` in Visual Studio 2022.
2. Configure `RaceDay.Api`'s `ConnectionStrings:DefaultConnection` to point to **your own** SQL Server and RaceDay database. Prefer local environment variables or User Secrets for credentials; **do not commit passwords**.
3. If this is an existing database created with legacy SQL scripts, review **Section 6 before enabling EF migrations**.
4. In Visual Studio, right-click the solution and choose **Configure Startup Projects → Multiple startup projects**. Set `RaceDay.Api` and `RaceDay.MVC` to **Start**; set the contracts and test projects to **None**.
5. Rebuild and press **F5**.
6. Open the local addresses below (the ports are from the committed launch profiles; check yours if changed).

| Service | URL |
| --- | --- |
| MVC website | http://localhost:5050 |
| API Swagger | https://localhost:59670/swagger |
| API health check | https://localhost:59670/api/health |
| Public event API | https://localhost:59670/api/events |

The MVC setting `RaceDayApi:BaseUrl` in `RaceDay.MVC/appsettings.json` must point to the **API HTTPS base URL**, not the MVC website. If Windows does not trust the local development certificate, run `dotnet dev-certs https --trust`.

**First-time functional check:** Register and log in as Organiser → create an event, category and route → log out → register/login as Participant → enrol → return as Organiser to capture results → check **My Results** as Participant.

## 6. SQL Server, EF Core and existing databases

Only the API accesses `RaceDayDbContext`. The API's Development configuration enables opt-in EF Core migration application through `RaceDay:AutoMigrate`. For a database already managed by EF migrations, pending migrations may be applied at API startup.

**If your tables were created by an older hand-written SQL script, make a backup first.** The legacy database might not have `Events.DistanceKm`, `Events.BannerImageUrl` or `Users.ProfileImageUrl`, and EF migration history may not match the physical tables.

- A symptom is `HTTP 500: Invalid column name 'DistanceKm'` during `GET /api/events`.
- Check that SSMS and the API both reference the **same SQL Server instance and database**.
- For a pre-existing database, review and use the **non-destructive** script: [`docs/RaceDay_ExistingDb_Part3_Repair.sql`](../docs/RaceDay_ExistingDb_Part3_Repair.sql).
- The repair adds required missing columns without dropping existing event and user records. Review any event whose distance remains `0`.
- **Do not rerun `docs/RaceDay_Database.sql` on an existing database:** the legacy script can drop/recreate tables and destroy data.
- Do not fabricate `__EFMigrationsHistory` entries to bypass migration problems. Reconcile schema and migrations before enabling automatic changes.

For a correctly configured **EF-managed** database, migrations can also be applied manually using the EF CLI:

~~~powershell
dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api
~~~

The SQL Server inside Docker is a **separate database** from the SQL Server instance used by your Visual Studio application.

## 7. Azure Blob Storage and Azurite

Part 3 adds image upload and retrieval, implemented in the API through `MediaController` and `BlobImageStore`. MVC sends the file to the API. The API validates images, stores file bytes in private Blob containers and saves a reference in SQL Server:

| Image | Private container | SQL field |
| --- | --- | --- |
| Event banner | `banners` | `Events.BannerImageUrl` |
| Profile photo | `profiles` | `Users.ProfileImageUrl` |

Supported formats: **JPG, PNG and WebP**, with a **5 MB maximum** and basic content/signature validation. MVC displays images through its `/media/{kind}/{name}` endpoint, which requests the media from the API. Blob containers are not publicly readable.

### Local Visual Studio/Azurite

`RaceDay.Api/appsettings.Development.json` contains `UseDevelopmentStorage=true`. Start Azurite on **127.0.0.1:10000** before uploading an image, then refresh the event/profile page to verify that the image is displayed. You may inspect the `banners` and `profiles` containers in Azure Storage Explorer.

If Azurite is unavailable, the application can still manage events and enrolments; image operations report a storage error (typically HTTP 503).

### Real Azure Storage (optional)

The same Blob SDK supports an actual Azure Storage account when configured securely through the API's `AzureStorage:ConnectionString`. Do not commit cloud storage access keys or connection strings. A real cloud account is **not necessary** to demonstrate the working local Azurite integration.

## 8. Run the full application using Docker

Docker Desktop and Docker Compose are required for this option. It starts four services: **`mvc`, `api`, `db` (SQL Server) and `azurite`**.

From the **repository root** in PowerShell:

~~~powershell
Copy-Item .env.example .env
~~~

Edit `.env`, replacing `MSSQL_SA_PASSWORD` with a strong local SQL Server password. Do **not** commit the `.env` file. Leave `AZURE_STORAGE_CONNECTION_STRING` empty to use the bundled Azurite emulator.

~~~powershell
docker compose up --build -d
docker compose ps
~~~

| Docker endpoint | URL/connection |
| --- | --- |
| MVC website | http://localhost:8080 |
| API health check | http://localhost:8081/api/health |
| Azurite Blob service (published on host) | http://127.0.0.1:10010 |
| MVC → API inside Docker | `http://api:8080/` |
| API → Azurite inside Docker | `http://azurite:10000` |

**Important port distinction:** The committed Docker Compose maps the container's Azurite port **10000 to host port 10010** to avoid conflicting with an independently running local emulator. Azure Storage Explorer must connect to the appropriate port for the Azurite instance being inspected.

The Docker SQL Server service is separate from your host SQL Server; create a new Organiser account and event inside Docker if your host-created event is missing. Docker Compose uses EF migrations to initialise its database.

To show a containerised event with an uploaded banner: open **http://localhost:8080** → sign in as an Organiser → create/edit an event → upload its banner → open the public event details page → refresh and confirm the image still displays.

Useful commands:

~~~powershell
docker compose logs api mvc azurite
docker compose down
~~~

`docker compose down` normally preserves data in named volumes. **Never use `docker compose down -v` if you need to keep your SQL Server or Blob Storage data.** If Docker cannot bind the SQL host port (often 1433), adjust only the left-hand host port mapping in your local `docker-compose.yml` without changing the container's port 1433.

## 9. Authentication and security

- Login and registration are performed by the API; passwords are hashed on the backend.
- The API creates a server-side `RaceDay.Session` cookie after successful login.
- MVC stores the corresponding API cookie in each visitor's own MVC session and forwards it with that visitor's API requests. Cookies are not shared between users.
- `RequireMvcRoleAttribute` restricts MVC pages; API `RequireRole` and ownership checks protect the actual data operations.
- Organisers can modify only events they own; participants can manage only their own enrolments.
- Logout clears the active sessions.
- Image uploads are restricted by file type, size and basic signature checks.

**Coursework limitation:** The session store is in memory, the registration flow allows users to choose the Organiser role, and Docker's development setup is not production-hardened. A production deployment would need controlled role provisioning, persisted session/data-protection keys, stricter security configuration and deployment-managed secrets.

## 10. Testing and CI/CD evidence

Automated tests can be run from the repository root:

~~~powershell
dotnet restore RaceDay.sln
dotnet build RaceDay.sln --configuration Release
dotnet test RaceDay.sln --configuration Release
~~~

The GitHub Actions workflow is [`.github/workflows/raceday-ci.yml`](../.github/workflows/raceday-ci.yml). On pushes and pull requests it:

1. Checks out the project and installs .NET 8.
2. Validates Docker Compose configuration.
3. Restores and builds the complete solution.
4. Compiles the MVC app and builds its Docker image.
5. Starts a local Azurite Blob emulator.
6. Runs API and MVC xUnit tests, including Blob upload/download round trips.

**Verified successful CI run (10 November 2026):** [RaceDay API and MVC Build and Test — run #121](https://github.com/OnceorTwiceaDay/RaceDayAPI.F/actions/runs/42853322552). It passed **62 API tests + 9 MVC tests = 71 automated tests**, and the Docker MVC image build completed successfully. This is the verified run prior to the final README documentation update; check [GitHub Actions](https://github.com/OnceorTwiceaDay/RaceDayAPI.F/actions) for newer results.

These automated checks are complemented by the manual Visual Studio, SSMS, MVC, Blob Storage and Docker demonstration. Automated CI tests do **not** prove that the application can connect to a particular student's local SQL Server.

## 11. Final submission screenshots and demonstration order

The recommended figures in the Part 3 report and video are:

| Figure | Evidence to show |
| --- | --- |
| **01** | Visual Studio Solution Explorer showing API, MVC, Contracts and both test projects |
| **02** | Swagger: `GET /api/events` (or `GET /api/health`) returning HTTP 200 |
| **03** | Organiser Dashboard displaying owned events and enrolment totals |
| **04** | Organiser event management: event, saved category and saved route |
| **05** | Participant's **My Enrolments** showing event, category and status |
| **06** | Participant's **My Results** showing a recorded time/position |
| **07** | SSMS query showing an event stored in the correct RaceDay SQL database |
| **08** | Uploaded event banner and profile image displayed in MVC |
| **09** | Azure Storage Explorer with `banners` and `profiles` containers |

**Additional Part 3 evidence:** Show (a) Docker Desktop running all four services; (b) the Docker MVC site at `http://localhost:8080` with an event and uploaded banner; and (c) a successful [GitHub Actions run](https://github.com/OnceorTwiceaDay/RaceDayAPI.F/actions). Capture screenshots from your actual project and avoid exposing passwords, session cookies or private participant details.

Suggested video flow: **solution → Swagger → Organiser → categories/routes → Participant enrolment → results → SQL data → displayed image → Blob containers → Docker website → GitHub CI**.

## 12. Troubleshooting

| Symptom | Check |
| --- | --- |
| MVC cannot contact API | Start both projects; verify `RaceDayApi:BaseUrl` and API HTTPS port |
| `307` redirect or `405` response | Confirm the correct HTTP/HTTPS scheme, host, port and endpoint |
| `401` or login cookie error | Log in again; ensure API `UseSession()` runs and cookies are preserved per MVC visitor |
| `403` access denied | Check current role and event/enrolment ownership |
| `500 Invalid column name` | Check the actual database schema; back up and apply the safe repair script if needed |
| Image not uploading or `503` | Start Azurite or configure accessible Azure Storage; verify content type/size |
| Docker MVC opens but event is missing | Docker uses a separate SQL database; create the event inside Docker |
| Docker host port conflict | Inspect `docker compose ps` and change the conflicting host-side published port |
| Changes not reflected locally | Run `git pull --ff-only origin master`, keeping local uncommitted configuration safe |

## 13. Supporting documentation and links

- [Repository home](https://github.com/OnceorTwiceaDay/RaceDayAPI.F)
- [Part 2 API documentation](../README.md)
- [RaceDay-specific Part 3 guide and requirement mapping](../docs/PROG6212_PART3_ADAPTED_GUIDE.md)
- [Safe legacy SQL database repair](../docs/RaceDay_ExistingDb_Part3_Repair.sql)
- [GitHub Actions CI/CD workflow](../.github/workflows/raceday-ci.yml)
- [Verified 71-test CI run](https://github.com/OnceorTwiceaDay/RaceDayAPI.F/actions/runs/42853322552)

## 14. Conclusion

Part 3 extends RaceDay into a full-stack, role-based ASP.NET Core solution. The Razor MVC application consumes the existing REST API for every persistent data operation, while the API remains responsible for SQL Server, authentication and image storage. The solution includes locally demonstrated organiser and participant workflows, Azure-compatible Blob Storage, Docker containerisation, and a verified passing GitHub Actions build/test workflow.

**Submission note:** Before handing in, confirm your own screenshots and demonstration recording, your environment's connection settings and the latest workflow status. Do not include `.env`, database passwords, Azure credentials or personal user data in the submission.
