using NetTopologySuite.Geometries;
using Skyhop.FlightAnalysis.Models;
using System;

namespace Skyhop.FlightAnalysis
{
    public abstract class Options
    {
        public Func<Point, double, PositionUpdate> NearbyAircraftAccessor { get; set; }
        public Func<Point, double, Point[]> NearbyRunwayAccessor { get; set; }

        public bool MinifyMemoryPressure { get; set; }

        public int MinimumRequiredPositionUpdateCount { get; set; } = 5;
    }
}
