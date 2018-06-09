using System;
namespace Boerman.FlightAnalysis.Models
{
    /// <summary>
    /// The flight metadata object contains information extracted from position
    /// updates. Position updates themselves are not included in this model.
    /// </summary>
    [Serializable]
    public class FlightMetadata
    {
        public FlightMetadata()
        {
        }

        public FlightMetadata(Flight flight)
        {
            Id = flight.Id;
            Aircraft = flight.Aircraft;
            LastSeen = flight.LastSeen;
            StartTime = flight.StartTime;
            DepartureHeading = flight.DepartureHeading;
            DepartureLocation = flight.DepartureLocation;
            DepartureInfoFound = flight.DepartureInfoFound;
            EndTime = flight.EndTime;
            ArrivalHeading = flight.ArrivalHeading;
            ArrivalLocation = flight.ArrivalLocation;
            ArrivalInfoFound = flight.ArrivalInfoFound;
        }

        public Guid? Id { get; set; }

        public string Aircraft { get; set; }

        public DateTime? LastSeen { get; set; }

        public DateTime? StartTime { get; set; }
        public short DepartureHeading { get; set; }
        public GeoCoordinate DepartureLocation { get; set; }
        public bool? DepartureInfoFound { get; set; }

        public DateTime? EndTime { get; set; }
        public short ArrivalHeading { get; set; }
        public GeoCoordinate ArrivalLocation { get; set; }
        public bool? ArrivalInfoFound { get; set; }

        public bool Completed => (DepartureInfoFound != null || StartTime != null) && (ArrivalInfoFound != null || EndTime != null);

        // ToDo: Make internal
        public Flight Flight => new Flight(this);
    }
}
