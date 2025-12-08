CREATE TABLE dbo.Airline(
    AirlineId INT IDENTITY PRIMARY KEY,
    IATACode NCHAR(2) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.Airport(
    AirportId INT IDENTITY PRIMARY KEY,
    IATACode NCHAR(3) NOT NULL UNIQUE,
    Name NVARCHAR(120) NOT NULL,
    City NVARCHAR(80) NULL,
    Country NVARCHAR(80) NULL,
    TimeZone NVARCHAR(64) NOT NULL
);

CREATE TABLE dbo.Gate(
    GateId INT IDENTITY PRIMARY KEY,
    AirportId INT NOT NULL REFERENCES dbo.Airport(AirportId),
    Code NVARCHAR(10) NOT NULL,
    UNIQUE(AirportId, Code)
);

CREATE TABLE dbo.Aircraft(
    AircraftId INT IDENTITY PRIMARY KEY,
    TailNumber NVARCHAR(10) NOT NULL UNIQUE,
    Model NVARCHAR(60) NOT NULL,
    SeatCapacity INT NOT NULL,
    OwnedByAirlineId INT NULL REFERENCES dbo.Airline(AirlineId),
    CHECK(SeatCapacity > 0)
);

CREATE TABLE dbo.Flight(
    FlightId INT IDENTITY PRIMARY KEY,
    AirlineId INT NOT NULL REFERENCES dbo.Airline(AirlineId),
    FlightNumber NVARCHAR(8) NOT NULL,
    OriginAirportId INT NOT NULL REFERENCES dbo.Airport(AirportId),
    DestinationAirportId INT NOT NULL REFERENCES dbo.Airport(AirportId),
    DefaultAircraftId INT NULL REFERENCES dbo.Aircraft(AircraftId),
    IsActive BIT NOT NULL DEFAULT 1,
    CHECK(OriginAirportId <> DestinationAirportId),
    CHECK(FlightNumber LIKE '[A-Z][A-Z]%' AND LEN(FlightNumber) >= 3)
);

CREATE TABLE dbo.FlightSchedule(
    FlightScheduleId INT IDENTITY PRIMARY KEY,
    FlightId INT NOT NULL REFERENCES dbo.Flight(FlightId),
    ScheduledDepartureUtc DATETIME2 NOT NULL,
    ScheduledArrivalUtc DATETIME2 NOT NULL,
    GateId INT NULL REFERENCES dbo.Gate(GateId),
    AssignedAircraftId INT NULL REFERENCES dbo.Aircraft(AircraftId),
    Status TINYINT NOT NULL,
    CHECK(ScheduledArrivalUtc > ScheduledDepartureUtc),
    CHECK(Status BETWEEN 0 AND 4)
);

CREATE TABLE dbo.Ticket(
    TicketId BIGINT IDENTITY PRIMARY KEY,
    FlightId INT NOT NULL REFERENCES dbo.Flight(FlightId),
    FareClass NVARCHAR(2) NOT NULL,
    BasePrice DECIMAL(10,2) NOT NULL,
    Taxes DECIMAL(10,2) NOT NULL,
    TotalPrice AS (BasePrice + Taxes) PERSISTED,
    Currency NCHAR(3) NOT NULL,
    IsRefundable BIT NOT NULL DEFAULT 0,
    SeatInventory INT NOT NULL,
    CHECK(BasePrice >= 0),
    CHECK(Taxes >= 0),
    CHECK(SeatInventory >= 0),
    CHECK(FareClass IN ('Y','M','J','F'))
);

CREATE TABLE dbo.Booking(
    BookingId BIGINT IDENTITY PRIMARY KEY,
    FlightScheduleId INT NOT NULL REFERENCES dbo.FlightSchedule(FlightScheduleId),
    TicketId BIGINT NOT NULL REFERENCES dbo.Ticket(TicketId),
    PassengerFullName NVARCHAR(120) NOT NULL,
    PassengerEmail NVARCHAR(120) NOT NULL,
    ConfirmationCode NVARCHAR(8) NOT NULL UNIQUE,
    Quantity INT NOT NULL,
    Status TINYINT NOT NULL,
    CreatedUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CHECK(Quantity > 0),
    CHECK(Status BETWEEN 0 AND 1),
    CHECK(LEN(ConfirmationCode) BETWEEN 6 AND 8
          AND ConfirmationCode NOT LIKE '%[^A-Z0-9]%')
);

CREATE INDEX IX_Flight_Airline_FlightNumber
ON dbo.Flight(AirlineId, FlightNumber);

CREATE INDEX IX_Flight_Active
ON dbo.Flight(AirlineId, FlightNumber)
WHERE IsActive = 1;

CREATE INDEX IX_Flight_Origin_Destination
ON dbo.Flight(OriginAirportId, DestinationAirportId);

CREATE INDEX IX_FlightSchedule_Flight_Departure
ON dbo.FlightSchedule(FlightId, ScheduledDepartureUtc);

CREATE INDEX IX_Ticket_Flight_FareClass
ON dbo.Ticket(FlightId, FareClass);