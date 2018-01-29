using System;
using System.Collections.Generic;

namespace Boerman.FlightAnalysis.Models
{
    public class Flight
    {
        public string Aircraft { get; internal set; }

        public DateTime? StartTime { get; internal set; }
        public short DepartureHeading { get; internal set; }
        public GeoCoordinate DepartureLocation { get; internal set; }
        public bool? DepartureInfoFound { get; internal set; }

        public DateTime? EndTime { get; internal set; }
        public short ArrivalHeading { get; internal set; }
        public GeoCoordinate ArrivalLocation { get; internal set; }
        public bool? ArrivalInfoFound { get; internal set; }

        public ICollection<PositionUpdate> PositionUpdates { get; internal set; }
    }
}
