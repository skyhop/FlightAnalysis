using GeoAPI.Geometries;
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
            if (flight == null) return;

            Id = flight.Id;
            Aircraft = flight.Aircraft;
            LastSeen = flight.LastSeen;
            DepartureTime = flight.StartTime;
            DepartureHeading = flight.DepartureHeading;
            DepartureLocation = flight.DepartureLocation;
            DepartureInfoFound = flight.DepartureInfoFound;
            ArrivalTime = flight.EndTime;
            ArrivalHeading = flight.ArrivalHeading;
            ArrivalLocation = flight.ArrivalLocation;
            ArrivalInfoFound = flight.ArrivalInfoFound;
        }

        public Guid? Id { get; set; }
        public string Aircraft { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime? DepartureTime { get; set; }
        public short DepartureHeading { get; set; }
        public IPoint DepartureLocation { get; set; }
        public bool? DepartureInfoFound { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public short ArrivalHeading { get; set; }
        public IPoint ArrivalLocation { get; set; }
        public bool? ArrivalInfoFound { get; set; }

        public bool Completed => (DepartureInfoFound != null || DepartureTime != null) && (ArrivalInfoFound != null || ArrivalTime != null);

        internal Flight Flight => new Flight(this);
    }
}
