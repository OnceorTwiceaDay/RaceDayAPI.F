# RaceDay Part 3: MVC Front End

The new `RaceDay.MVC` .NET 8 project calls the existing `RaceDay.Api` over HTTP. MVC does **not** reference EF Core or access SQL Server directly.

## Local setup

1. Install .NET 8 SDK and SQL Server.
2. Configure the existing API connection string with user secrets or environment variables.
3. Apply migrations:

   `dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api`

4. Start the API. Confirm its HTTP URL from `RaceDay.Api/Properties/launchSettings.json`. Example:

   `dotnet run --project RaceDay.Api`

5. Set `RaceDayApi__BaseUrl` to the API's actual HTTP URL (default: `http://localhost:59671/`).
6. Launch MVC:

   `dotnet run --project RaceDay.MVC --launch-profile http`

7. Open `http://localhost:5050`.

## Docker Compose

Copy `.env.example` to `.env` and replace the SQL Server password. Set `AZURE_STORAGE_CONNECTION_STRING` if uploading images. Run:

`docker compose up --build -d`

Docker Compose enables the API's opt-in startup migrations and retries while SQL Server becomes ready. For local development this makes startup a single command; do not enable automatic migrations for production without a controlled deployment plan. Browse `http://localhost:8080`; API lives at `http://localhost:8081`.

For manual migrations, install the EF CLI and point `ConnectionStrings__DefaultConnection` at your SQL Server. Avoid committing credentials.

## Session integration

API authentication uses the original Part 2 `RaceDay.Session` session cookie. MVC saves that API cookie separately inside each visitor's MVC session; server-to-server calls use `HttpClientHandler.UseCookies=false` and attach the current visitor's API cookie. Logout invalidates API and MVC sessions.

## Uploaded images

The API receives images, validates basic headers, and stores image bytes in private Azure Blob containers `banners` and `profiles`. Set `AzureStorage__ConnectionString` via environment or user secrets. MVC sends multipart uploads to the API and proxies read requests; it has no Azure credential. Uploaded image URLs are API-relative paths stored in SQL Server. JPG, PNG and WebP up to 5 MB are supported. Image access is public through the API once linked to an event/profile.

## Pages

- Public home, sport/search filtering, event details, categories, routes and leaderboard.
- Participant account, event enrolment, cancellation, enrolment history, personal results, profile/photo.
- Organiser dashboard, event CRUD, event banner, category creation/removal, entrant status and result publishing.

## Limitations / planned refinements

- The Part 2 API allows users to register as organisers; production deployment should verify that role.
- Session storage is in-memory, suitable only for a single local instance.
- Routes are displayed but editing routes in MVC is not included yet.
- Category edit UI is not included yet (API endpoint already exists).
- Results do not yet show a reliable total-finishers denominator (e.g. 47th of 312).
- Azure Blob credentials and SQL migrations are required for full operation; do not commit secrets.
- This branch was authored through GitHub integration. Confirm compilation and unit tests in GitHub Actions before submission.

## GitHub Actions

The workflow `.github/workflows/raceday-ci.yml` restores and builds `RaceDay.sln` including MVC and runs the existing API tests.
