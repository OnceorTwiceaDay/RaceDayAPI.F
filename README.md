# prog6212-part-2-Kanya-12

# RaceDay Event Management System Part2

1. *Project Description:*

- RaceDay is a full-stack event management system for the South African road running, walking, and cycling community. Event organisers create and manage events, categories, routes, and results, while participants browse events, enrol in categories, and track their own performance history.

This repository contains the Part 2 implementation for PROG6212 (Programming 2B), including the ASP.NET Core Web API, EF Core model, Swagger API, unit tests, and CI/CD workflow. The Part 1 planning documents remain in the /docs folder and are still the specification for the project foundation.

2. *User Roles*

A) *Organiser:*

- Registers and logs in as an organiser.
- Creates, edits, and deletes their own events.
- Manages categories and routes for their own events.
- Views all enrolments for their own events and can change an enrolment status.
- Captures and corrects finish times and finishing positions for their own events.
- Cannot enrol in events or edit another organiser's events.

B) *Participant:*

- Registers and logs in as a participant.
- Browses events, categories, and routes.
- Enrols in an event by choosing a category.
- Views and cancels their own enrolments.
- Views their own results and performance history.
- Cannot create, edit, or delete events, categories, routes, or results.

Both roles can view and update their own profile.

3. *Part 1 Deliverables*


## Part 1 Deliverables

| Deliverable | File |
|---|---|
| Entity Relationship Diagram | [RaceDay_ERD.pdf](https://github.com/RCNMBCampus/prog6212-part-1-Kanya-12/blob/ce7c92df92390fc9564417d4c0ef1fcf9eb84456/docs/RaceDay_ERD.pdf) |
| API Endpoint Plan | [RaceDay_API_Endpoint_Plan.pdf](https://github.com/RCNMBCampus/prog6212-part-1-Kanya-12/blob/ce7c92df92390fc9564417d4c0ef1fcf9eb84456/docs/RaceDay_API_Endpoint_Plan.pdf) |
| SQL Database Script | [RaceDay_Database.sql](https://github.com/RCNMBCampus/prog6212-part-1-Kanya-12/blob/ce7c92df92390fc9564417d4c0ef1fcf9eb84456/docs/RaceDay_Database.sql) |

The database has six entities: User, Event, Category, Route, Enrolment, and Result. Enrolment is the associative entity resolving the many-to-many relationship between Participants and Events via a chosen Category. Result has a 1:0..1 relationship with Enrolment because a result only exists once the organiser captures it.

The Part 1 workflow (.github/workflows/part1-ci.yml) checks that the /docs folder and required files exist.

## Part 1 CI screenshot:

<img width="813" height="54" alt="image" src="https://github.com/user-attachments/assets/06b55ae6-450c-409b-bdab-0b66e0aae5cb" />

## Successful build screenshot:

<img width="898" height="54" alt="image" src="https://github.com/user-attachments/assets/dcaeff56-6205-4a48-85fe-27f0fff2af01" />

## Part 1 video (unlisted):
- https://youtu.be/DlgdIhYUSgA

## Part 2

*Technologies Used:*
- ASP.NET Core 8 Web API (C#), built with Visual Studio 2022
- Entity Framework Core 8, Code-First, with SQL Server
- Server-side sessions for authentication (AddSession)
- ASP.NET Core PasswordHasher for password hashing
- Swashbuckle (Swagger UI) with XML documentation comments
- xUnit, Microsoft.AspNetCore.Mvc.Testing and the EF Core in-memory provider for tests
- GitHub Actions for CI/CD

## Project Structure

```text
RaceDay/
├── RaceDay.sln
├── docs/                         # Part 1 planning documents
├── RaceDay.Api/                  # ASP.NET Core Web API
│   ├── Controllers/              # Auth, Profile, Events, Categories,
│   │                             # Routes, Enrolments, Results
│   ├── Data/                     # RaceDayDbContext
│   ├── Filters/                  # RequireRoleAttribute (role enforcement)
│   ├── Migrations/               # Entity Framework Core migrations
│   ├── Models/                   # AppUser, Event, Category, RaceRoute,
│   │                             # Enrolment, Result, Role
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.json
│   └── Program.cs
├── RaceDay.Contracts/            # Shared request and response DTOs
│   └── Dtos.cs
├── RaceDay.Api.Tests/            # xUnit automated tests
├── .github/
│   └── workflows/
│       ├── part1-ci.yml
│       └── part2-ci.yml
└── README.md
```

## Database Setup:
1. You need a SQL Server instance. LocalDB is installed with Visual Studio; SQL Server Express also works.
2. Open RaceDay.Api/appsettings.json and set the Server value in DefaultConnection to your own instance. It must keep the doubled backslash inside the JSON file.
3. Create the database from the committed migrations using Visual Studio or the terminal with `dotnet ef database update --project RaceDay.Api`.
4. Open SQL Server Management Studio and expand RaceDayDb → Tables to confirm the Users, Events, Categories, Routes, Enrolments, Results, and __EFMigrationsHistory tables exist.

The API does not seed any data. Register accounts through Swagger as described below.

The docs/RaceDay_Database.sql script from Part 1 creates a separate database called RaceDay; the Part 2 database is RaceDayDb and is generated from EF Core migrations.

## How to Run the API

*Visual Studio:*
1. Open RaceDay.sln.
2. Right-click RaceDay.Api → Set as Startup Project.
3. Choose the https profile and press F5.
4. Swagger opens at https://localhost:7236/swagger.

Terminal:

```bash
dotnet run --project RaceDay.Api --launch-profile https
```

Then browse to https://localhost:7236/swagger.

*Authentication*

- Register (POST /api/auth/register) validates the request, checks the role is Organiser or Participant, rejects duplicate email addresses with 409, hashes the password, and saves the user.
- Login (POST /api/auth/login) verifies the password against the stored hash and starts a server-side session.
- Logout (POST /api/auth/logout) clears the session.
- Role enforcement is done by RequireRoleAttribute and returns 401 or 403 as needed.
- Ownership checks ensure organisers and participants only access the records they own.
- Sessions are held in server memory, so restarting the API logs everyone out.

*API Endpoint Reference*

| Method | Route | Role | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | None | Create an Organiser or Participant account |
| POST | `/api/auth/login` | None | Verify credentials and start a session |
| POST | `/api/auth/logout` | Any | Clear the session |
| GET | `/api/users/me` | Any | View own profile |
| PUT | `/api/users/me` | Any | Update own profile |
| GET | `/api/events` | None | List all events |
| GET | `/api/events/{id}` | None | View one event |
| GET | `/api/events/mine` | Organiser | List the organiser's own events |
| POST | `/api/events` | Organiser | Create an event |
| PUT | `/api/events/{id}` | Organiser (owner) | Update an event |
| DELETE | `/api/events/{id}` | Organiser (owner) | Delete an event |
| GET | `/api/events/{eventId}/routes` | None | List an event's routes |
| POST | `/api/events/{eventId}/routes` | Organiser (owner) | Add a route to an event |
| GET | `/api/events/{eventId}/categories` | None | List an event's categories |
| POST | `/api/events/{eventId}/categories` | Organiser (owner) | Add a category to an event |
| PUT | `/api/categories/{id}` | Organiser (owner) | Update a category |
| DELETE | `/api/categories/{id}` | Organiser (owner) | Delete a category |
| POST | `/api/events/{eventId}/enrolments` | Participant | Enrol in an event under a category |
| GET | `/api/users/me/enrolments` | Participant | View own enrolments |
| GET | `/api/events/{eventId}/enrolments` | Organiser (owner) | View all enrolments for an event |
| PUT | `/api/enrolments/{id}/status` | Organiser (owner) | Set an enrolment to Pending, Confirmed, or Cancelled |
| DELETE | `/api/enrolments/{id}` | Participant (owner) | Cancel own enrolment |
| POST | `/api/enrolments/{enrolmentId}/result` | Organiser (owner) | Capture a finish time and position |
| PUT | `/api/results/{id}` | Organiser (owner) | Correct a captured result |
| GET | `/api/events/{eventId}/results` | None | View an event's results |
| GET | `/api/users/me/results` | Participant | View own results |

That is 26 endpoints: the 24 in the Part 1 plan plus the two added in Part 2.

*Part 2 Changes and Improvements*

| Change | Reason |
|---|---|
| Login creates a session instead of returning a JWT | Part 2 requires session-based authentication. |
| Added POST /api/auth/logout | Required to clear a session. |
| Added DistanceKM to Event | Every event must capture distance. |
| Added GET /api/events/mine | Allows organisers to see only their own events. |
| Added PUT /api/enrolments/{id}/status | Allows organisers to confirm or cancel an entrant. |
| AppUser and Users table instead of User | USER is a reserved word in T-SQL. |
| RaceRoute class (table still called Routes) | Avoids a naming clash with ASP.NET Core routing. |
| Role is a C# enum stored as text in Users.Role | Provides type safety while matching the SQL script. |

No entities, attributes, keys, or relationships differ between the ERD, the SQL script, and the EF Core model.


## Swagger

Swagger UI is available at /swagger while the API runs in the Development environment. Every endpoint is listed and can be tested directly in the browser, grouped by controller (Auth, Profile, Events, Categories, Routes, Enrolments, Results). Endpoint descriptions come from the XML comments on each controller action.

## Unit Testing

The RaceDay.Api.Tests project contains 54 xUnit tests. Each test class starts the real API in memory using WebApplicationFactory and swaps SQL Server for a temporary in-memory database so the tests never touch the real database.

## Automated Tests

| Test class | What it covers |
|---|---|
| `AuthTests` | Registration, login, logout, and profile tests. |
| `EventTests` | Public viewing and organiser validation tests. |
| `RoleAccessTests` | Protected endpoint access checks. |
| `EnrolmentTests` | Enrolment creation, ownership, and validation tests. |
| `ResultTests` | Capturing and viewing participant results. |

Run the tests in Visual Studio or with:

```bash
dotnet test RaceDay.sln
```

All 54 tests pass locally and in GitHub Actions.

## CI/CD

.github/workflows/part2-ci.yml runs on every push and pull request. It checks out the repository, installs .NET 8, restores packages, builds RaceDay.sln in Release mode, and runs all unit tests. The tests use an in-memory database, so the workflow does not need SQL Server.

.github/workflows/part1-ci.yml also still runs and checks the Part 1 documents.

Successful build screenshot For Par2 CI:

<img width="818" height="45" alt="image" src="https://github.com/user-attachments/assets/b3eb33e5-9a2c-4992-9537-8c4f4730f8a6" />


## Video Presentation

Part 2 YouTube link (unlisted):
https://youtu.be/AqaLe4g8zug
