using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace Skyhop.FlightAnalysis.Experimental
{
    public abstract class Options
    {
        public Func<(Point coordinate, double distance), IEnumerable<FlightContext>> NearbyAircraftAccessor { get; set; }
        public Func<(Point coordinate, double distance), Point[]> NearbyRunwayAccessor { get; set; }

        public bool MinifyMemoryPressure { get; set; }

        public int MinimumRequiredPositionUpdateCount { get; set; } = 5;
    }
}
