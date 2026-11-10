# PROG6212 Part 3 — RaceDay implementation and demonstration

This checklist **adapts** the supplied 55-page *PROG6212 Part 3: ASP.NET Core MVC – Simple Step-by-Step Notes* to the RaceDay event management coursework. The slides demonstrate a DIFFERENT Course Registration API with JWT Bearer authentication and the roles **Admin/Lecturer/Student**. **RaceDay uses Organiser/Participant and the session-based Part 2 authentication already implemented**. Do not paste the sample JWT middleware, Course endpoints or AddCourseDto into RaceDay: it would break this project's established API contract.

## Requirements mapped to RaceDay

| Class notes | RaceDay implementation | What to demonstrate |
| --- | --- | --- |
| pp. 2–3: MVC → API → SQL Server | \`RaceDay.MVC\` calls \`RaceDay.Api\` via \`RaceDayApiClient\`; ONLY the API uses \`RaceDayDbContext\` | Create data through MVC and show the saved record in SSMS |
| pp. 4–8: Confirm Swagger/login first | API Swagger, \`/api/health\`, \`api/auth/register\`, \`api/auth/login\`, role filters | Successful login and rejected incorrect password |
| pp. 9–15: .NET 8 MVC, services and views | \`RaceDay.sln\`, \`RaceDay.MVC/Program.cs\`, controllers, models, Razor views and \`wwwroot\` | Both API and MVC launch from Visual Studio |
| pp. 16–23: API login and MVC session | MVC POSTs email/password to the API, stores the per-user **RaceDay.Session cookie** and role in MVC session | Sign in as Organiser and Participant |
| pp. 24–28: Role dashboard/navigation and logout | \`OrganiserController\`, \`ParticipantController\`, \`RequireMvcRoleAttribute\`, \`_Layout.cshtml\`, \`AccountController.Logout\` | Each role sees correct links; logged-out protected pages redirect |
| pp. 29–36: Read/Create through API | Search events, event details, organiser event + category + route creation via API | Show event created through MVC in API and SSMS |
| pp. 37–39: errors and Bootstrap | HTTP error diagnostics and Bootstrap + \`site.css\` | Wrong password and wrong-role behaviour |
| pp. 40–50: Azurite and Blob Storage | \`BlobImageStore\` + \`MediaController\`; \`Users.ProfileImageUrl\` and \`Events.BannerImageUrl\` via EF migration | Upload event banner / profile photo, verify emulator containers, reload and display images |
| pp. 51–53: Docker | \`RaceDay.MVC/Dockerfile\`, \`RaceDay.Api/Dockerfile\`, \`docker-compose.yml\` + Azurite service | Run containerized MVC and API, create event and display image |
| p. 54: Final test checklist | Manual procedure below + API/MVC tests + Azurite CI checks | Screenshots/video of complete flow |

**Why no bearer token?** The class notes are a *general example*. The RaceDay Part 2 API authenticates with an ASP.NET Core session cookie and stores the role server-side; its MVC front end forwards that cookie per visitor. It is the equivalent authentication flow for this existing project; using \`Authorization: Bearer\` would **not** authenticate against this API. Confirm with your lecturer if they insist on JWT specifically rather than the architecture in the RaceDay assignment.

## A. Start the projects in Visual Studio 2022

1. Install the **.NET 8 SDK**, SQL Server (e.g. SQL Server Express) and Visual Studio ASP.NET workload.
2. Open \`RaceDay.sln\`; set **RaceDay.Api** and **RaceDay.MVC** to **Start** in *Configure Startup Projects*.
3. Ensure \`RaceDay.Api/appsettings.json\` (or local User Secrets) has a \`ConnectionStrings:DefaultConnection\` pointing to YOUR SQL Server, not another person's lab PC. Do not commit private credentials.
4. If needed: \`dotnet dev-certs https --trust\`.
5. Start both with F5. API: \`https://localhost:59670/swagger\`; MVC: \`http://localhost:5050\`. If Visual Studio assigned a different API HTTPS port, update \`RaceDay.MVC/appsettings.json\` key \`RaceDayApi:BaseUrl\`.
6. Open \`https://localhost:59670/api/health\` and verify it returns JSON. Login uses \`POST /api/auth/login\`.

**Database migrations:** Development mode enables EF Core migrations automatically for databases created/managed by EF. Make a backup first. If you created the tables by executing \`docs/RaceDay_Database.sql\` without EF migration history, consult the actual schema and \`__EFMigrationsHistory\` before starting automatic migrations; they can conflict with existing tables. Avoid dropping existing user data.

## B. Start local Azurite (the image-storage requirement)

The API development config uses:

\`\`\`json
"AzureStorage": { "ConnectionString": "UseDevelopmentStorage=true" }
\`\`\`

This uses the standard local Azurite storage emulator; it is **not** a production cloud credential. Start Azurite using **one** of these methods:

- Visual Studio: open **Connected Services** on the API and add the *Storage Azurite emulator* service dependency, or start Azurite if already installed with Visual Studio.
- Docker Desktop (host-only development): \`docker run --rm --name raceday-azurite -p 127.0.0.1:10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0\`
- npm: install/run Azurite according to Microsoft Learn; it must listen on \`127.0.0.1:10000\`.

With the emulator running, upload a JPG, PNG or WebP (max 5 MB) from **Organiser → Create/Edit Event** or **My Profile → Save Profile**. Data stays in SQL Server; the bytes go to the \`banners\` or \`profiles\` Blob container. Refresh the MVC page to verify the image loads from \`/media/banners/{name}\` or \`/media/profiles/{name}\`. Use **Azure Storage Explorer → Local & Attached → Storage Accounts → Emulator → Blob Containers** to inspect \`banners\` and \`profiles\`. Verify both upload **and** display, as the notes emphasize on pp. 48–49.

If no emulator runs, image endpoints return a helpful **HTTP 503**, while the rest of the site remains usable. For a real cloud submission, override the connection string with API User Secrets or \`AzureStorage__ConnectionString\`, using a **real** Azure Storage connection string. Do not push secrets to GitHub.

## C. Test the full RaceDay flow

1. In MVC, register a NEW Organiser account; log in with the same password. Repeat with an incorrect password to verify a rejection.
2. Organiser dashboard: create an event (e.g. Pretoria 10K, future date), one category and a route; optionally upload its event banner.
3. Open \`/api/events\` in Swagger and the \`Events\` table in SSMS. Verify that the newly created event is listed.
4. Log out. Register and log in as a **Participant** using a different email. Browse/search the event, choose its category and submit an enrolment. Review **My enrolments**.
5. Return as Organiser; open **Manage** and confirm the Participant appears. Update enrolment status and publish the finisher's time and position.
6. As Participant, open **My results**. Verify the result, profile editing and profile image upload.
7. Try accessing an organiser management endpoint while logged in as Participant. The API must deny the request even if someone sends it manually.
8. Upload an image, refresh the page and verify both the SQL path/reference and the blob bytes in Azurite/Storage Explorer.
9. Capture screenshots of Swagger, MVC dashboards, SQL results, Storage Explorer, and Docker for your practical evidence.

**Known scope limitation:** RaceDay stores \`PaymentStatus\` but does not collect real payment. No payment-gateway integration is supplied by these class notes.

## D. Test Docker Compose

From the repository root:

\`\`\`powershell
Copy-Item .env.example .env
# Edit .env to use a strong MSSQL_SA_PASSWORD. Leave AZURE_STORAGE_CONNECTION_STRING blank for the local Azurite default.
docker compose up --build -d
docker compose ps
\`\`\`

Open **MVC http://localhost:8080**, **API http://localhost:8081/api/health**, and the Azurite emulator on **127.0.0.1:10000**. Inside Docker, the API connects to \`azurite:10000\`, and MVC connects to \`api:8080\`. **Do not use localhost as a container-to-container destination** (the class notes' Docker warning on p. 52).

Create an event and upload/display its banner. Inspect containers with \`docker compose logs api mvc azurite\`. Stop with \`docker compose down\` (data remains in named volumes); \`docker compose down -v\` DELETES the SQL/blob volumes, so avoid it if you need test data.

## E. Git conflicts after pulling into Visual Studio

If \`git status\` says **merge in progress** or **unmerged paths**, your local repository cannot accept another pull until you finish that merge. The remote GitHub branch cannot resolve conflicts in your working directory:

1. Open *View → Git Changes → Merge Conflicts*.
2. Review each local/incoming result; do not automatically overwrite the local SQL connection string or other uncommitted work.
3. For \`RaceDay.MVC/appsettings.json\`, use the HTTPS API URL matching YOUR running API; for \`RaceDay.Api/appsettings.json\`, preserve YOUR SQL Server name.
4. Mark each conflict resolved, stage it, and complete the merge commit in Visual Studio.
5. Once \`git status\` confirms no unmerged paths, pull the latest \`master\` again if Git reports more changes.

If you don't know which version of a conflicting file is correct, **save the \`git status\` output and ask before accepting either side**. Never run \`git reset --hard\` or \`git clean -fd\` merely to clear conflicts: these can destroy local work.

## F. Evidence vs automated tests

GitHub Actions compiles the complete .NET 8 solution and runs both API and MVC automated tests. With the CI Azurite service running, additional integration tests confirm that valid image bytes are uploaded through the API, stored in the Blob emulator, retrieved again and recorded in the user/event database rows.

These tests are not a substitute for manually verifying your existing SQL Server database, Visual Studio startup, Docker Desktop and Azure Storage Explorer. For assessed work, capture the manual screenshots requested by your lecturer.
