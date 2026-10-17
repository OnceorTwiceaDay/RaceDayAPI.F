# RaceDay Event Management System — Part 2

**Module:** PROG6212 — 
**Project:** RaceDay Event Management System  

## 1. About the Project

RaceDay is a web-based event management application developed for South Africa's road running, walking, and cycling community. It provides a central platform where event organisers can manage sporting events and participants can discover events, register for activities, and review their results.

The application allows organisers to create and maintain events, configure participation categories, manage routes, oversee enrolments, and record finishing positions and times. Participants can browse available events, select a category, manage their enrolments, and access their personal performance records.

This repository contains the Part 2 implementation for PROG6212. The solution includes an ASP.NET Core Web API, Entity Framework Core data models, Swagger documentation, automated unit tests, and a GitHub Actions CI/CD workflow.

The planning documents completed for Part 1 are retained in the `docs` directory and continue to provide the original project specifications.

## 2. Application Users and Permissions

RaceDay supports two primary user roles. Each role has specific permissions to ensure users can access the functionality relevant to their responsibilities.

### 2.1 Event Organiser

Organisers are responsible for creating and administering sporting events.

Their available functions include:

- Creating an account and signing in with the Organiser role.
- Creating, updating, and deleting events they own.
- Adding and maintaining event categories and routes.
- Viewing enrolments associated with their events.
- Updating enrolment statuses.
- Recording finishing times and positions for participants.
- Correcting previously recorded results.

Organisers cannot enrol as participants or modify events belonging to other organisers. Ownership checks are applied to the relevant operations.

### 2.2 Participant

Participants use RaceDay to find events and manage their involvement.

Their available functions include:

- Registering and signing in as a Participant.
- Viewing available events, categories, and routes.
- Enrolling in an event by selecting a suitable category.
- Viewing their own enrolments and cancelling them when necessary.
- Viewing their results and personal performance history.

Participants are not permitted to create, modify, or delete events, categories, routes, or results.

### 2.3 Profile Management

Both Organisers and Participants can view and update their own profile information.

## 3. Technologies and Development Tools

The application uses the following technologies and frameworks:

| Technology | Purpose |
|---|---|
| ASP.NET Core 8 Web API | Provides the application's HTTP API and server-side functionality. |
| C# | Implements application logic, controllers, and data models. |
| Visual Studio 2022 | Development and debugging environment. |
| Entity Framework Core 8 | Handles database access using the Code-First approach. |
| SQL Server | Stores application data. |
| ASP.NET Core Session | Maintains authenticated user sessions. |
| ASP.NET Core PasswordHasher | Hashes passwords before they are stored. |
| Swashbuckle / Swagger UI | Documents and allows interactive testing of API endpoints. |
| xUnit | Runs automated unit and integration-style tests. |
| Microsoft.AspNetCore.Mvc.Testing | Hosts the API in a test environment. |
| EF Core In-Memory Provider | Provides a temporary database for automated tests. |
| GitHub Actions | Automates building and testing through CI/CD workflows. |

## 4. Solution Structure

The repository is organised into separate projects and folders to keep the API, shared data-transfer objects, automated tests, and planning documents easy to maintain.

```text
RaceDay/
├── RaceDay.sln
├── docs/                         # Part 1 planning documents
├── RaceDay.Api/                  # ASP.NET Core Web API
│   ├── Controllers/              # Auth, Profile, Events, Categories,
│   │                             # Routes, Enrolments, Results
│   ├── Data/                     # RaceDayDbContext
│   ├── Filters/                  # RequireRoleAttribute
│   ├── Migrations/               # EF Core database migrations
│   ├── Models/                   # AppUser, Event, Category, RaceRoute,
│   │                             # Enrolment, Result, Role
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.json
│   └── Program.cs
├── RaceDay.Contracts/            # Shared request and response DTOs
│   └── Dtos.cs
├── RaceDay.Api.Tests/            # Automated xUnit tests
├── .github/
│   └── workflows/
│       ├── part1-ci.yml
│       └── part2-ci.yml
└── README.md
```

## 5. Database Configuration

RaceDay uses SQL Server with Entity Framework Core migrations to create and update the Part 2 database.

### Prerequisites

Before running the application, ensure that you have:

- Visual Studio 2022 or a suitable .NET development environment.
- The .NET 8 SDK.
- SQL Server LocalDB, SQL Server Express, or another compatible SQL Server instance.
- Access to the repository and its solution file.

### Configuration steps

1. Open `RaceDay.Api/appsettings.json`.
2. Locate the `DefaultConnection` connection string.
3. Change the server name to match your installed SQL Server instance. Preserve the escaped backslashes required by JSON when using a Windows instance name.
4. Open a terminal in the solution directory.
5. Apply the committed Entity Framework Core migrations by running:

   ```bash
   dotnet ef database update --project RaceDay.Api
   ```

6. Open SQL Server Management Studio and connect to the configured SQL Server instance.
7. Expand the `RaceDayDb` database and inspect its tables.

The database should contain the following application tables:

- `Users`
- `Events`
- `Categories`
- `Routes`
- `Enrolments`
- `Results`
- `__EFMigrationsHistory`

**Important database distinction:** The SQL script from Part 1, `docs/RaceDay_Database.sql`, creates a separate database named `RaceDay`. The Part 2 API uses `RaceDayDb`, which is created and maintained through Entity Framework Core migrations. These are separate database setup approaches.

The application does not automatically insert sample records. Create user accounts through the registration endpoint in Swagger before testing authenticated functionality.

## 6. Starting the Application

### Option A: Run through Visual Studio

1. Open `RaceDay.sln` in Visual Studio 2022.
2. In Solution Explorer, right-click `RaceDay.Api`.
3. Select **Set as Startup Project**.
4. Select the HTTPS launch profile.
5. Press **F5** to build and run the application.
6. Open the Swagger interface at:

https://localhost:59670/swagger/index.html

### Option B: Run through the terminal

From the directory containing the solution, execute:

```bash
dotnet run --project RaceDay.Api --launch-profile https
```

Once the API starts, access Swagger at:

https://localhost:59670/swagger/index.html

The displayed port may differ if the HTTPS launch profile has been configured differently.

## 7. Authentication and Access Control

RaceDay uses session-based authentication to manage access to protected API operations.

### Account registration

`POST /api/auth/register`

The registration process validates the submitted information, accepts the Organiser or Participant role, checks for duplicate email addresses, hashes the password, and saves the new account. A duplicate email address results in an HTTP `409 Conflict` response.

### User login

`POST /api/auth/login`

The login operation checks the submitted credentials against the stored password hash. If authentication succeeds, the API starts a server-side session.

### User logout

`POST /api/auth/logout`

This endpoint clears the active session and signs the user out.

### Role and ownership validation

The `RequireRoleAttribute` filter protects endpoints that require a particular role. Requests without valid authentication or sufficient permissions receive the appropriate HTTP `401 Unauthorized` or `403 Forbidden` response.

Additional ownership checks prevent users from modifying records belonging to other users. For example, an organiser may only manage their own events, while a participant may only cancel their own enrolments.

**Session limitation:** Sessions are maintained in server memory. Restarting the API clears the active sessions, requiring users to sign in again.

## 8. API Endpoint Guide

The following table summarises the application's 26 API endpoints, including their HTTP methods, access requirements, and intended functions.

| Method | Endpoint | Access | Function |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register an Organiser or Participant. |
| POST | `/api/auth/login` | Public | Validate credentials and start a session. |
| POST | `/api/auth/logout` | Authenticated | End the current session. |
| GET | `/api/users/me` | Authenticated | Retrieve the current user's profile. |
| PUT | `/api/users/me` | Authenticated | Update the current user's profile. |
| GET | `/api/events` | Public | Retrieve all available events. |
| GET | `/api/events/{id}` | Public | Retrieve a particular event. |
| GET | `/api/events/mine` | Organiser | List events owned by the signed-in organiser. |
| POST | `/api/events` | Organiser | Create a new event. |
| PUT | `/api/events/{id}` | Event owner | Update an existing event. |
| DELETE | `/api/events/{id}` | Event owner | Delete an owned event. |
| GET | `/api/events/{eventId}/routes` | Public | List routes belonging to an event. |
| POST | `/api/events/{eventId}/routes` | Event owner | Add a route to an owned event. |
| GET | `/api/events/{eventId}/categories` | Public | List categories for an event. |
| POST | `/api/events/{eventId}/categories` | Event owner | Add a category to an owned event. |
| PUT | `/api/categories/{id}` | Category's event owner | Update a category. |
| DELETE | `/api/categories/{id}` | Category's event owner | Delete a category. |
| POST | `/api/events/{eventId}/enrolments` | Participant | Enrol in a category for an event. |
| GET | `/api/users/me/enrolments` | Participant | View the participant's own enrolments. |
| GET | `/api/events/{eventId}/enrolments` | Event owner | View enrolments for an owned event. |
| PUT | `/api/enrolments/{id}/status` | Event owner | Change an enrolment to Pending, Confirmed, or Cancelled. |
| DELETE | `/api/enrolments/{id}` | Enrolment owner | Cancel the participant's own enrolment. |
| POST | `/api/enrolments/{enrolmentId}/result` | Event owner | Record a finishing time and position. |
| PUT | `/api/results/{id}` | Event owner | Correct a recorded result. |
| GET | `/api/events/{eventId}/results` | Public | View results for an event. |
| GET | `/api/users/me/results` | Participant | View the participant's own results. |

The API includes 26 endpoints in total: the 24 described in the original Part 1 plan and two additional endpoints introduced during Part 2 development.

## 9. Changes Introduced in Part 2

Several implementation decisions were made to align the application with the Part 2 requirements and improve its functionality.

| Change | Explanation |
|---|---|
| Session-based login | Authentication now creates a server-side session instead of returning a JWT. |
| Logout endpoint | `POST /api/auth/logout` was added to clear the current session. |
| Event distance | A `DistanceKM` property was added to the Event model so event distances can be recorded. |
| Organiser event listing | `GET /api/events/mine` allows organisers to retrieve their own events. |
| Enrolment status management | `PUT /api/enrolments/{id}/status` allows organisers to update an entrant's status. |
| `AppUser` model and `Users` table | The application uses `AppUser` as its C# model name and `Users` as its table name to avoid conflicts associated with the SQL Server `USER` keyword. |
| `RaceRoute` model | The C# class is named `RaceRoute`, while the database table remains `Routes`, avoiding confusion with ASP.NET Core routing concepts. |
| Role enumeration | The Role is represented by a C# enum and stored as text in `Users.Role`, providing type safety while maintaining compatibility with the SQL script. |

The intended database entities, attributes, primary keys, foreign keys, and relationships remain consistent across the ERD, the Part 1 SQL script, and the Entity Framework Core model.

## 10. Swagger API Documentation

Swagger UI provides an interactive interface for exploring and testing the API.

When the application is running in the Development environment, Swagger is available at `/swagger`.

The endpoints are grouped according to their controllers:

- Authentication
- Profile
- Events
- Categories
- Routes
- Enrolments
- Results

XML documentation comments provide descriptions for the controller actions. Through Swagger, developers can inspect request parameters, submit API requests, and review the returned responses.

For protected operations, register and log in first so that the required session is established.

## 11. Automated Testing

The `RaceDay.Api.Tests` project contains 54 xUnit tests covering important application behaviour, validation rules, authentication, and access permissions.

Tests use `WebApplicationFactory` to run the API in an in-memory test host. SQL Server is replaced with an Entity Framework Core In-Memory database, allowing the tests to run without accessing the application's real database.

### Test coverage

| Test class | Main areas tested |
|---|---|
| `AuthTests` | Account registration, login, logout, and profile operations. |
| `EventTests` | Public event access and organiser-related validation. |
| `RoleAccessTests` | Access restrictions on protected endpoints. |
| `EnrolmentTests` | Enrolment creation, validation, and ownership rules. |
| `ResultTests` | Recording and viewing participant results. |

### Running the tests

You can execute the test suite from Visual Studio's Test Explorer or run the following command in the solution directory:

```bash
dotnet test RaceDay.sln
```

The project's reported local test result is that all 54 tests pass. The GitHub Actions workflow is also configured to execute the automated tests. Confirm the latest workflow run to verify the current result.

## 12. Continuous Integration and Continuous Delivery (CI/CD)

RaceDay uses GitHub Actions to automate build and test checks whenever changes are pushed to the repository or a pull request is created.

The Part 2 workflow is located at:

```text
.github/workflows/part2-ci.yml
```

Its configured tasks are:

1. Check out the repository.
2. Install the .NET 8 SDK.
3. Restore the required NuGet packages.
4. Build `RaceDay.sln` using the Release configuration.
5. Execute the automated tests.

Because the tests use an in-memory database, the workflow does not require a running SQL Server instance.

The repository also contains a separate workflow:

```text
.github/workflows/part1-ci.yml
```

This workflow checks the Part 1 documentation.

### CI build evidence

Include a screenshot of a successful Part 2 GitHub Actions workflow run here.

*Suggested screenshot caption: Figure 1 — Successful execution of the RaceDay Part 2 CI workflow.*

## 13. Video Demonstration

The video presentation demonstrates the completed Part 2 implementation.

**YouTube presentation link (unlisted):**

[Insert the Part 2 YouTube video URL here]

Ensure the link is accessible to the intended reviewer before submitting the project.

## 14. Conclusion

The RaceDay Event Management System provides a structured API for managing sporting events, participant enrolments, routes, categories, and results. Its implementation combines ASP.NET Core, Entity Framework Core, and SQL Server with session-based authentication, role-based permissions, and ownership validation.

The addition of automated testing and GitHub Actions provides a repeatable way to check the solution during development. Together with the Part 1 planning documents, the Part 2 implementation establishes the application's data structure, API functionality, and testing workflow.
