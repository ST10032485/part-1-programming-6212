/* =====================================================
   RaceDay Database Script (SQL Server / SSMS)
   Part 1 - System Planning and Database
   Run the whole script on a clean SQL Server instance.
   ===================================================== */

IF DB_ID('RaceDayDB') IS NULL
    CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* ---------- Drop tables (child tables first) so the script can be re-run ---------- */
DROP TABLE IF EXISTS Results;
DROP TABLE IF EXISTS Enrolments;
DROP TABLE IF EXISTS EventCategories;
DROP TABLE IF EXISTS Events;
DROP TABLE IF EXISTS Categories;
DROP TABLE IF EXISTS Users;
DROP TABLE IF EXISTS Roles;
GO

/* ---------- 1. Roles ---------- */
CREATE TABLE Roles (
    RoleID      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    NVARCHAR(30) NOT NULL UNIQUE
);

/* ---------- 2. Users ---------- */
CREATE TABLE Users (
    UserID        INT IDENTITY(1,1) PRIMARY KEY,
    RoleID        INT NOT NULL,
    FirstName     NVARCHAR(50)  NOT NULL,
    LastName      NVARCHAR(50)  NOT NULL,
    Email         NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(255) NOT NULL,
    Phone         NVARCHAR(20)  NULL,
    DateOfBirth   DATE          NULL,
    CreatedAt     DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);

/* ---------- 3. Categories (race types such as 5K, 10K) ---------- */
CREATE TABLE Categories (
    CategoryID    INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName  NVARCHAR(50)   NOT NULL UNIQUE,
    DistanceKm    DECIMAL(5,2)   NULL,   -- empty for age categories such as Under 20
    Description   NVARCHAR(255)  NULL,
    CONSTRAINT CK_Categories_Distance CHECK (DistanceKm IS NULL OR DistanceKm > 0)
);

/* ---------- 4. Events (created by an Organiser) ---------- */
CREATE TABLE Events (
    EventID               INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID           INT           NOT NULL,
    Title                 NVARCHAR(100) NOT NULL,
    Description           NVARCHAR(500) NULL,
    Venue                 NVARCHAR(100) NOT NULL,
    City                  NVARCHAR(50)  NOT NULL,
    EventDate             DATE          NOT NULL,
    RegistrationDeadline  DATE          NOT NULL,
    EventType             NVARCHAR(10)  NOT NULL DEFAULT 'Run',
    DistanceKm            DECIMAL(5,2)  NOT NULL DEFAULT 5,
    Status                NVARCHAR(20)  NOT NULL DEFAULT 'Upcoming',
    CreatedAt             DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserID) REFERENCES Users(UserID),
    CONSTRAINT CK_Events_Status CHECK (Status IN ('Upcoming', 'Completed', 'Cancelled')),
    CONSTRAINT CK_Events_Deadline CHECK (RegistrationDeadline <= EventDate),
    CONSTRAINT CK_Events_Type CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    CONSTRAINT CK_Events_Distance CHECK (DistanceKm > 0)
);

/* ---------- 5. EventCategories (links Events and Categories, many-to-many) ---------- */
CREATE TABLE EventCategories (
    EventCategoryID  INT IDENTITY(1,1) PRIMARY KEY,
    EventID          INT           NOT NULL,
    CategoryID       INT           NOT NULL,
    EntryFee         DECIMAL(10,2) NOT NULL DEFAULT 0,
    MaxParticipants  INT           NOT NULL DEFAULT 100,
    CONSTRAINT FK_EventCategories_Events FOREIGN KEY (EventID) REFERENCES Events(EventID),
    CONSTRAINT FK_EventCategories_Categories FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_EventCategories UNIQUE (EventID, CategoryID),
    CONSTRAINT CK_EventCategories_Fee CHECK (EntryFee >= 0),
    CONSTRAINT CK_EventCategories_Max CHECK (MaxParticipants > 0)
);

/* ---------- 6. Enrolments (a Participant joins an event category) ---------- */
CREATE TABLE Enrolments (
    EnrolmentID      INT IDENTITY(1,1) PRIMARY KEY,
    UserID           INT          NOT NULL,
    EventCategoryID  INT          NOT NULL,
    BibNumber        INT          NOT NULL,
    EnrolmentDate    DATETIME2    NOT NULL DEFAULT SYSDATETIME(),
    Status           NVARCHAR(20) NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_Enrolments_EventCategories FOREIGN KEY (EventCategoryID) REFERENCES EventCategories(EventCategoryID),
    CONSTRAINT UQ_Enrolments_User_EventCategory UNIQUE (UserID, EventCategoryID),
    CONSTRAINT UQ_Enrolments_Bib UNIQUE (EventCategoryID, BibNumber),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Confirmed', 'Cancelled'))
);

/* ---------- 7. Results (one result per enrolment) ---------- */
CREATE TABLE Results (
    ResultID     INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID  INT          NOT NULL UNIQUE,
    FinishTime   TIME(0)      NULL,
    Position     INT          NULL,
    Status       NVARCHAR(20) NOT NULL DEFAULT 'Finished',
    RecordedAt   DATETIME2    NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID),
    CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished', 'DNF', 'DNS')),
    CONSTRAINT CK_Results_Position CHECK (Position IS NULL OR Position > 0)
);
GO

/* =====================================================
   SEED DATA
   Note: PasswordHash values are placeholders. Part 2 will
   store real hashed passwords created by the API.
   ===================================================== */

INSERT INTO Roles (RoleName) VALUES
('Organiser'),
('Participant');

INSERT INTO Users (RoleID, FirstName, LastName, Email, PasswordHash, Phone, DateOfBirth) VALUES
(1, 'Thabo',  'Mokoena', 'thabo.mokoena@raceday.test',  'HASH_PLACEHOLDER_1', '0821110001', '1985-03-14'),
(1, 'Lerato', 'Dlamini', 'lerato.dlamini@raceday.test', 'HASH_PLACEHOLDER_2', '0821110002', '1988-07-02'),
(2, 'Sipho',  'Nkosi',   'sipho.nkosi@raceday.test',    'HASH_PLACEHOLDER_3', '0821110003', '1996-11-21'),
(2, 'Aisha',  'Patel',   'aisha.patel@raceday.test',    'HASH_PLACEHOLDER_4', '0821110004', '1999-05-09'),
(2, 'Daniel', 'Smith',   'daniel.smith@raceday.test',   'HASH_PLACEHOLDER_5', '0821110005', '1992-01-30');

INSERT INTO Categories (CategoryName, DistanceKm, Description) VALUES
('5K Fun Run',    5.00,  'Beginner friendly fun run'),
('10K Road Race', 10.00, 'Standard 10 kilometre road race'),
('Half Marathon', 21.10, 'Full half marathon distance'),
('Trail 15K',     15.00, 'Off road trail route'),
('Under 20',       NULL,  'Age category for athletes under 20'),
('Senior',         NULL,  'Age category for senior athletes');

INSERT INTO Events (OrganiserID, Title, Description, Venue, City, EventDate, RegistrationDeadline, Status, EventType, DistanceKm) VALUES
(1, 'City 10K Challenge',     'Annual city centre road race',          'Central Park',        'Johannesburg', '2026-08-22', '2026-08-15', 'Completed', 'Run', 10.00),
(1, 'Spring Trail Run',       'Scenic trail run through the hills',    'Hillside Reserve',    'Cape Town',    '2026-11-14', '2026-11-07', 'Upcoming', 'Run', 15.00),
(2, 'Beachfront Half Marathon','Flat and fast course along the beach', 'Golden Mile Promenade','Durban',       '2026-12-06', '2026-11-29', 'Upcoming', 'Run', 21.10);

INSERT INTO EventCategories (EventID, CategoryID, EntryFee, MaxParticipants) VALUES
(1, 1, 100.00, 200),   -- City 10K: 5K Fun Run
(1, 2, 150.00, 300),   -- City 10K: 10K Road Race
(2, 1, 120.00, 150),   -- Spring Trail: 5K Fun Run
(2, 4, 200.00, 100),   -- Spring Trail: Trail 15K
(3, 2, 180.00, 250),   -- Beachfront: 10K Road Race
(3, 3, 250.00, 250);   -- Beachfront: Half Marathon

INSERT INTO Enrolments (UserID, EventCategoryID, BibNumber, Status) VALUES
(3, 2, 101, 'Confirmed'),   -- Sipho, City 10K, 10K
(4, 2, 102, 'Confirmed'),   -- Aisha, City 10K, 10K
(5, 1, 21,  'Confirmed'),   -- Daniel, City 10K, 5K
(3, 4, 301, 'Confirmed'),   -- Sipho, Spring Trail, 15K
(4, 6, 501, 'Confirmed'),   -- Aisha, Beachfront, Half Marathon
(5, 5, 401, 'Confirmed');   -- Daniel, Beachfront, 10K

INSERT INTO Results (EnrolmentID, FinishTime, Position, Status) VALUES
(1, '00:48:32', 1, 'Finished'),
(2, '00:55:10', 2, 'Finished'),
(3, '00:29:45', 1, 'Finished');
GO

/* ---------- Quick check ---------- */
SELECT 'Roles' AS TableName, COUNT(*) AS TotalRows FROM Roles
UNION ALL SELECT 'Users', COUNT(*) FROM Users
UNION ALL SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL SELECT 'Events', COUNT(*) FROM Events
UNION ALL SELECT 'EventCategories', COUNT(*) FROM EventCategories
UNION ALL SELECT 'Enrolments', COUNT(*) FROM Enrolments
UNION ALL SELECT 'Results', COUNT(*) FROM Results;
GO
