using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace Skyhop.FlightAnalysis
{
    public class FlightContextOptions : Options
    {
        internal Func<Point, double, IEnumerable<FlightContext>> NearbyAircraftAccessor { get; set; }
        internal Func<string, FlightContext> AircraftAccessor { get; set; }

        public string AircraftId { get; set; }
    }
}
