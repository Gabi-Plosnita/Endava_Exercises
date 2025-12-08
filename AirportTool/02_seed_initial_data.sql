INSERT INTO dbo.Airline(IATACode, Name)
VALUES
    ('AA', 'Alpha Air'),
    ('BB', 'Bravo Blue');

INSERT INTO dbo.Airport(IATACode, Name, City, Country, TimeZone)
VALUES
    ('AAA', 'Alpha International Airport', 'Alpha City', 'CountryA', 'UTC'),
    ('BBB', 'Bravo Regional Airport', 'Bravo City', 'CountryB', 'UTC');

INSERT INTO dbo.Gate(AirportId, Code)
VALUES
    (1, 'A1'),
    (1, 'A2'),
    (1, 'A3');

INSERT INTO dbo.Gate(AirportId, Code)
VALUES
    (2, 'B1'),
    (2, 'B2'),
    (2, 'B3');

INSERT INTO dbo.Aircraft(TailNumber, Model, SeatCapacity, OwnedByAirlineId)
VALUES
    ('N100AA', 'Boeing 737-800', 180, 1); 

INSERT INTO dbo.Flight(
    AirlineId, FlightNumber,
    OriginAirportId, DestinationAirportId,
    DefaultAircraftId, IsActive
)
VALUES
    (1, 'AA100', 1, 2, 1, 1),  
    (2, 'BB200', 2, 1, 1, 1);   

INSERT INTO dbo.FlightSchedule(
    FlightId, ScheduledDepartureUtc, ScheduledArrivalUtc,
    GateId, AssignedAircraftId, Status
)
VALUES
    (1, '2025-01-01T08:00:00', '2025-01-01T10:00:00', 1, 1, 0),  
    (2, '2025-01-01T12:00:00', '2025-01-01T14:00:00', 4, 1, 0);  

INSERT INTO dbo.Ticket(
    FlightId, FareClass, BasePrice, Taxes, Currency,
    IsRefundable, SeatInventory
)
VALUES
    (1, 'Y', 100.00, 20.00, 'USD', 0, 50),   
    (1, 'J', 300.00, 40.00, 'USD', 1, 10);   

INSERT INTO dbo.Ticket(
    FlightId, FareClass, BasePrice, Taxes, Currency,
    IsRefundable, SeatInventory
)
VALUES
    (2, 'Y', 90.00, 18.00, 'USD', 0, 60);
