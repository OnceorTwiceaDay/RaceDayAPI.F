/* =====================================================================
   RaceDay Database Script
   PROG6212 PoE Part 1 - Section C

   Run this entire script top-to-bottom in SSMS on a fresh SQL Server
   instance. It creates its own database, so no manual setup is required.
   ===================================================================== */

-- ---------------------------------------------------------------------
-- 0. Create and select the database
-- ---------------------------------------------------------------------
IF DB_ID('RaceDay') IS NULL
BEGIN
    CREATE DATABASE RaceDay;
END
GO

USE RaceDay;
GO

-- ---------------------------------------------------------------------
-- 1. Drop tables if they already exist (child tables first), so this
--    script can be re-run safely while you are developing/testing it.
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.Results', 'U')     IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U')  IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Routes', 'U')      IS NOT NULL DROP TABLE dbo.Routes;
IF OBJECT_ID('dbo.Categories', 'U')  IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U')      IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U')       IS NOT NULL DROP TABLE dbo.Users;
GO

-- ---------------------------------------------------------------------
-- 2. CREATE TABLE statements (one per ERD entity)
-- ---------------------------------------------------------------------

-- Entity: User  ->  Table: Users
CREATE TABLE Users (
    UserID          INT IDENTITY(1,1)   PRIMARY KEY,
    FullName        NVARCHAR(100)       NOT NULL,
    Email           NVARCHAR(150)       NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)       NOT NULL,
    Role            NVARCHAR(20)        NOT NULL
                        CHECK (Role IN ('Organiser','Participant')),
    PhoneNumber     NVARCHAR(20)        NULL,
    DateOfBirth     DATE                NULL,
    RegisteredDate  DATETIME            NOT NULL DEFAULT GETDATE()
);
GO

-- Entity: Event  ->  Table: Events
CREATE TABLE Events (
    EventID         INT IDENTITY(1,1)   PRIMARY KEY,
    OrganiserID     INT                 NOT NULL,
    EventName       NVARCHAR(150)       NOT NULL,
    Description     NVARCHAR(1000)      NULL,
    EventDate       DATE                NOT NULL,
    Location        NVARCHAR(150)       NOT NULL,
    EventType       NVARCHAR(20)        NOT NULL
                        CHECK (EventType IN ('Running','Walking','Cycling')),
    CreatedDate     DATETIME            NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID)
        REFERENCES Users(UserID)
);
GO

-- Entity: Category  ->  Table: Categories
CREATE TABLE Categories (
    CategoryID      INT IDENTITY(1,1)   PRIMARY KEY,
    EventID         INT                 NOT NULL,
    CategoryName    NVARCHAR(100)       NOT NULL,
    DistanceKM      DECIMAL(5,2)        NOT NULL CHECK (DistanceKM > 0),
    MinAge          INT                 NOT NULL DEFAULT 0,
    MaxAge          INT                 NULL,
    EntryFee        DECIMAL(8,2)        NOT NULL DEFAULT 0 CHECK (EntryFee >= 0),
    CONSTRAINT FK_Categories_Event FOREIGN KEY (EventID)
        REFERENCES Events(EventID)
);
GO

-- Entity: Route  ->  Table: Routes
CREATE TABLE Routes (
    RouteID         INT IDENTITY(1,1)   PRIMARY KEY,
    EventID         INT                 NOT NULL,
    RouteName       NVARCHAR(100)       NOT NULL,
    DistanceKM      DECIMAL(5,2)        NOT NULL CHECK (DistanceKM > 0),
    StartPoint      NVARCHAR(150)       NOT NULL,
    EndPoint        NVARCHAR(150)       NOT NULL,
    ElevationGainM  INT                 NULL DEFAULT 0,
    MapURL          NVARCHAR(255)       NULL,
    CONSTRAINT FK_Routes_Event FOREIGN KEY (EventID)
        REFERENCES Events(EventID)
);
GO

-- Entity: Enrolment  ->  Table: Enrolments
CREATE TABLE Enrolments (
    EnrolmentID     INT IDENTITY(1,1)   PRIMARY KEY,
    ParticipantID   INT                 NOT NULL,
    EventID         INT                 NOT NULL,
    CategoryID      INT                 NOT NULL,
    EnrolmentDate   DATETIME            NOT NULL DEFAULT GETDATE(),
    Status          NVARCHAR(20)        NOT NULL DEFAULT 'Confirmed'
                        CHECK (Status IN ('Pending','Confirmed','Cancelled')),
    PaymentStatus   NVARCHAR(20)        NOT NULL DEFAULT 'Unpaid'
                        CHECK (PaymentStatus IN ('Unpaid','Paid','Refunded')),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantID)
        REFERENCES Users(UserID),
    CONSTRAINT FK_Enrolments_Event FOREIGN KEY (EventID)
        REFERENCES Events(EventID),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (CategoryID)
        REFERENCES Categories(CategoryID),
    -- A participant may only hold one active enrolment per event
    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantID, EventID)
);
GO

-- Entity: Result  ->  Table: Results
CREATE TABLE Results (
    ResultID          INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID       INT               NOT NULL UNIQUE, -- enforces 1:1 with Enrolments
    FinishTime        TIME              NULL,
    FinishPosition    INT               NULL,
    CategoryPosition  INT               NULL,
    Status            NVARCHAR(20)      NOT NULL DEFAULT 'Finished'
                          CHECK (Status IN ('Finished','DNF','DNS')),
    RecordedDate      DATETIME          NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolments(EnrolmentID)
);
GO

-- ---------------------------------------------------------------------
-- 3. Seed data
--    IDs below are deterministic: the DROP+CREATE above resets every
--    IDENTITY column to 1, and rows are inserted in the exact order
--    listed, so these comments reliably describe the resulting IDs.
-- ---------------------------------------------------------------------

-- Users: 2 Organisers (1-2), 2 Participants (3-4)
-- PasswordHash values below are placeholder hashes for seed/testing
-- purposes only, not real credentials.
INSERT INTO Users (FullName, Email, PasswordHash, Role, PhoneNumber, DateOfBirth) VALUES
('Thandiwe Kupula',   'thandiwe.kupula@raceday-events.co.za', 'AQAAAAEAACcQAAAAEPlaceholderHash1==', 'Organiser',   '0823264747', '1980-03-14'), -- UserID 1
('Johan Peter','johan.peter@raceday-events.co.za','AQAAAAEAACcQAAAAEPlaceholderHash2==', 'Organiser',   '0837654321', '1981-11-02'), -- UserID 2
('Lerato Jack',       'lerato.jack@gmail.com',                'AQAAAAEAACcQAAAAEPlaceholderHash3==', 'Participant', '0712345675', '1995-06-21'), -- UserID 3
('Lucas Botha',        'lucas.botha@gmail.com',                 'AQAAAAEAACcQAAAAEPlaceholderHash4==', 'Participant', '0768885499', '1999-01-09'); -- UserID 4
GO

-- Events: 3 events, owned by the two Organisers above
INSERT INTO Events (OrganiserID, EventName, Description, EventDate, Location, EventType) VALUES
(1, 'Cape Town Cycle Tour',            'A timed cycling event taking riders around the Cape Peninsula.',        '2026-03-08', 'Cape Town, Western Cape',     'Cycling'), -- EventID 1
(1, 'Soweto Marathon',                 'A marathon through the historic streets of Soweto, Gauteng.',           '2026-11-01', 'Soweto, Gauteng',             'Running'), -- EventID 2
(2, 'Stellenbosch Vineyard Charity Run','A community charity run and walk through the Stellenbosch winelands.', '2026-09-20', 'Stellenbosch, Western Cape',  'Running'); -- EventID 3
GO

-- Categories: at least one per event (7 total)
INSERT INTO Categories (EventID, CategoryName, DistanceKM, MinAge, MaxAge, EntryFee) VALUES
(1, '109km Individual',    109.00, 18, NULL, 650.00), -- CategoryID 1
(1, '56km Half Cycle',      56.00, 14, NULL, 450.00), -- CategoryID 2
(2, '42.2km Marathon',      42.20, 20, NULL, 350.00), -- CategoryID 3
(2, '21.1km Half Marathon', 21.10, 16, NULL, 250.00), -- CategoryID 4
(2, '10km Fun Run',         10.00, 12, NULL, 150.00), -- CategoryID 5
(3, '10km Run',             10.00, 14, NULL, 120.00), -- CategoryID 6
(3, '5km Walk/Run',          5.00,  8, NULL,  80.00); -- CategoryID 7
GO

-- Routes: at least one per event (3 total)
INSERT INTO Routes (EventID, RouteName, DistanceKM, StartPoint, EndPoint, ElevationGainM, MapURL) VALUES
(1, 'Full Peninsula Route',  109.00, 'Hertzog Boulevard, Cape Town CBD',    'Green Point Stadium',                 1200, 'https://raceday.co.za/routes/cape-town-cycle-tour'),      -- RouteID 1
(2, 'Soweto Streets Route',   42.20, 'FNB Stadium',                        'Elkah Stadium',                         350, 'https://raceday.co.za/routes/soweto-marathon'),           -- RouteID 2
(3, 'Vineyard Loop',          10.00, 'Stellenbosch Botanical Garden',      'Stellenbosch Botanical Garden',          90, 'https://raceday.co.za/routes/stellenbosch-charity-run');  -- RouteID 3
GO

-- Enrolments: 4 sample enrolments across both participants and all three events
INSERT INTO Enrolments (ParticipantID, EventID, CategoryID, Status, PaymentStatus) VALUES
(3, 1, 1, 'Confirmed', 'Paid'),   -- EnrolmentID 1: Lerato -> Cape Town Cycle Tour, 109km
(3, 2, 4, 'Confirmed', 'Paid'),   -- EnrolmentID 2: Lerato -> Soweto Marathon, 21.1km Half
(4, 2, 3, 'Confirmed', 'Unpaid'), -- EnrolmentID 3: David  -> Soweto Marathon, 42.2km Marathon
(4, 3, 6, 'Confirmed', 'Paid');   -- EnrolmentID 4: David  -> Stellenbosch Charity Run, 10km
GO

-- Results: seeded for two enrolments to demonstrate the optional
-- (0..1) relationship - not every enrolment has a result yet.
INSERT INTO Results (EnrolmentID, FinishTime, FinishPosition, CategoryPosition, Status) VALUES
(1, '04:35:12', 245, 38, 'Finished'), -- Lerato's Cape Town Cycle Tour result
(3, '03:58:47', 512, 90, 'Finished'); -- David's Soweto Marathon result
GO

-- ---------------------------------------------------------------------
-- 4. Quick verification queries (optional - comment out before final
--    submission if you only want the schema + seed data to run)
-- ---------------------------------------------------------------------
-- SELECT * FROM Users;
-- SELECT * FROM Events;
-- SELECT * FROM Categories;
-- SELECT * FROM Routes;
-- SELECT * FROM Enrolments;
-- SELECT * FROM Results;
