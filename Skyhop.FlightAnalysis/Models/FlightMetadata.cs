using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using System;

namespace Skyhop.FlightAnalysis.Models
{
    // ToDo: Map the departure/arrival location in an object so that the model becomes clearer (no more smurf typing)

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
            DepartureTime = flight.StartTime;
            DepartureHeading = flight.DepartureHeading;

            DepartureLongitude = flight.DepartureLocation?.X ?? 0;
            DepartureLatitude = flight.DepartureLocation?.Y ?? 0;

            DepartureInfoFound = flight.DepartureInfoFound;

            LaunchMethod = flight.LaunchMethod;

            ArrivalTime = flight.EndTime;
            ArrivalHeading = flight.ArrivalHeading;

            ArrivalLongitude = flight.ArrivalLocation?.X ?? 0;
            ArrivalLatitude = flight.ArrivalLocation?.Y ?? 0;

            ArrivalInfoFound = flight.ArrivalInfoFound;
        }

        public Guid? Id { get; set; }
        public string Aircraft { get; set; }
        public DateTime? DepartureTime { get; set; }
        public short DepartureHeading { get; set; }

        [JsonIgnore]
        public Point DepartureLocation => new Point(DepartureLongitude, DepartureLatitude);

        public double DepartureLatitude { get; set; }
        public double DepartureLongitude { get; set; }

        public bool? DepartureInfoFound { get; set; }

        public LaunchMethods LaunchMethod { get; set; }

        public DateTime? ArrivalTime { get; set; }
        public short ArrivalHeading { get; set; }

        [JsonIgnore]
        public Point ArrivalLocation => new Point(ArrivalLongitude, ArrivalLatitude);

        public double ArrivalLatitude { get; set; }
        public double ArrivalLongitude { get; set; }

        public bool? ArrivalInfoFound { get; set; }

        public bool Completed => (DepartureInfoFound != null || DepartureTime != null) && (ArrivalInfoFound != null || ArrivalTime != null);

        internal Flight Flight => new Flight(this);
    }
}
