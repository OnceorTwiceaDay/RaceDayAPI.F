/*
    RaceDay Part 3 -- NON-DESTRUCTIVE repair for an existing database.

    WHY:
      The Part 1 docs/RaceDay_Database.sql script does NOT create
      Events.DistanceKm. The current EF model expects it, plus the Part 3
      nullable Events.BannerImageUrl and Users.ProfileImageUrl columns.

    IMPORTANT:
      1. Back up RaceDayDb first.
      2. Confirm that the API's DefaultConnection points to THIS database.
      3. Run in SSMS only against the intended RaceDayDb instance.
      4. This script preserves existing accounts, events and enrolments.
      5. Do NOT run the old drop-and-recreate RaceDay_Database.sql to fix this.
      6. This is a one-time schema reconciliation for legacy manual SQL
         databases, NOT a substitute for checking __EFMigrationsHistory.
*/

USE [RaceDayDb];
GO

SELECT
    @@SERVERNAME AS ConnectedServer,
    DB_NAME() AS ConnectedDatabase,
    OBJECT_ID(N'dbo.Events', N'U') AS EventsTableObjectId,
    OBJECT_ID(N'dbo.Users', N'U') AS UsersTableObjectId;
GO

IF OBJECT_ID(N'dbo.Events', N'U') IS NULL
    THROW 50001, 'The dbo.Events table does not exist in RaceDayDb. Verify the API connection string before changing the database.', 1;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    THROW 50002, 'The dbo.Users table does not exist in RaceDayDb. Verify the API connection string before changing the database.', 1;

DECLARE @AddedDistanceKm BIT = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Missing from the original Part 1 Events table.
    IF COL_LENGTH(N'dbo.Events', N'DistanceKm') IS NULL
    BEGIN
        ALTER TABLE dbo.Events
            ADD DistanceKm DECIMAL(6,2) NOT NULL
            CONSTRAINT DF_RaceDay_Events_DistanceKm DEFAULT (0) WITH VALUES;

        SET @AddedDistanceKm = 1;
        PRINT 'Added dbo.Events.DistanceKm; existing rows initially use 0.';
    END
    ELSE
        PRINT 'dbo.Events.DistanceKm already exists. No change made.';

    -- Part 3: EF model expects nullable image references in the database.
    IF COL_LENGTH(N'dbo.Events', N'BannerImageUrl') IS NULL
    BEGIN
        ALTER TABLE dbo.Events
            ADD BannerImageUrl NVARCHAR(300) NULL;
        PRINT 'Added dbo.Events.BannerImageUrl.';
    END

    IF COL_LENGTH(N'dbo.Users', N'ProfileImageUrl') IS NULL
    BEGIN
        ALTER TABLE dbo.Users
            ADD ProfileImageUrl NVARCHAR(300) NULL;
        PRINT 'Added dbo.Users.ProfileImageUrl.';
    END

    -- Populate newly-added event headline distances using the largest
    -- category distance where category records are available.
    -- Existing DistanceKm data is never overwritten.
    -- Dynamic SQL avoids same-batch compilation errors after ADD COLUMN.
    IF @AddedDistanceKm = 1
       AND OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.Categories', N'DistanceKM') IS NOT NULL
    BEGIN
        EXEC sys.sp_executesql N'
            UPDATE ev
               SET ev.DistanceKm = c.MaxDistance
              FROM dbo.Events AS ev
              JOIN (
                    SELECT EventID, MAX(DistanceKM) AS MaxDistance
                      FROM dbo.Categories
                     GROUP BY EventID
                   ) AS c ON c.EventID = ev.EventID
             WHERE ev.DistanceKm = 0
               AND c.MaxDistance > 0;
        ';
        PRINT 'Inferred headline distances from the longest category of each existing event.';
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- Verify schema. The API also expects several other columns defined by
-- its EF Core migrations, so review any subsequent Invalid column errors.
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE
  FROM INFORMATION_SCHEMA.COLUMNS
 WHERE TABLE_SCHEMA = N'dbo'
   AND TABLE_NAME IN (N'Events', N'Users', N'Categories')
   AND COLUMN_NAME IN
       (N'DistanceKm', N'BannerImageUrl', N'ProfileImageUrl')
 ORDER BY TABLE_NAME, COLUMN_NAME;

-- For events without categories, 0 is a visible placeholder.
-- Edit those values to their real distances before using the event.
SELECT EventID, EventName, DistanceKm
  FROM dbo.Events
 ORDER BY EventID;

-- DO NOT insert rows into __EFMigrationsHistory manually. If the API
-- still fails during automatic EF migration, inspect its migration history
-- and reconcile the whole schema separately, preserving existing data.
