using System;
using System.Linq;

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
            StartTime = flight.StartTime;
            DepartureHeading = flight.DepartureHeading;
            DepartureLocation = flight.DepartureLocation?.ToString();
            DepartureInfoFound = flight.DepartureInfoFound;
            EndTime = flight.EndTime;
            ArrivalHeading = flight.ArrivalHeading;
            ArrivalLocation = flight.ArrivalLocation?.ToString();
            ArrivalInfoFound = flight.ArrivalInfoFound;
        }

        public Guid? Id { get; set; }
        public string Aircraft { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime? StartTime { get; set; }
        public short DepartureHeading { get; set; }
        public string DepartureLocation { get; set; }
        public bool? DepartureInfoFound { get; set; }
        public DateTime? EndTime { get; set; }
        public short ArrivalHeading { get; set; }
        public string ArrivalLocation { get; set; }
        public bool? ArrivalInfoFound { get; set; }

        // Some helper methods
        public double[] DepartureCoordinate => DepartureLocation?.Split(',')?.Select(q => Double.Parse(q)).ToArray();
        public double[] ArrivalCoordinate => ArrivalLocation?.Split(',')?.Select(q => Double.Parse(q)).ToArray();
        public bool Completed => (DepartureInfoFound != null || StartTime != null) && (ArrivalInfoFound != null || EndTime != null);

        // ToDo: Make internal
        public Flight Flight => new Flight(this);
    }
}
