using NetTopologySuite.Geometries;
using Skyhop.FlightAnalysis.Models;
using System;

namespace Skyhop.FlightAnalysis
{
    public class FlightContextOptions
    {
        public Func<Point, double, PositionUpdate> NearbyAircraftAccessor { get; set; }
        public Func<Point, double, Point[]> NearbyRunwayAccessor { get; set; }

        public int MinimumRequiredPositionUpdateCount { get; set; } = 5;
        public bool MinifyMemoryPressure { get; set; } = false;
        
        public string AircraftId { get; set; }
    }
}
