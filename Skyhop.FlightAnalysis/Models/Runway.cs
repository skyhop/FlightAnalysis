using Boerman.Core.Spatial;

namespace Skyhop.FlightAnalysis.Models
{
    

    public class Runway {
        public class Side
        {
            public Point Location { get; set; }
            public int Elevation { get; set; }
        }

        public Runway(Side oneEnd, Side otherEnd)
        {
            Sides = new[] { oneEnd, otherEnd };
        }

        internal readonly Side[] Sides;
    }
}
