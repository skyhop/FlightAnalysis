using NetTopologySuite.Geometries;

namespace Skyhop.FlightAnalysis.Models
{
    

    public class Runway {
        public Runway(Point oneEnd, Point otherEnd)
        {
            Sides = new[] { oneEnd, otherEnd };
        }

        internal readonly Point[] Sides;
    }
}
