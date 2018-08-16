using System;
using System.Linq;
using System.Threading.Tasks;

namespace Boerman.FlightAnalysis.FlightStates
{
    /// <summary>
    /// The FindArrivalHeading state is invoked once a landing is detected. Sole purpose of this class is to determine
    /// the heading during landing. Heading can later on be used to determine the active runway.
    /// </summary>
    public class FindArrivalHeading : FlightState
    {
        public FindArrivalHeading(FlightContext context) : base(context)
        {
        }

        public override async Task Run()
        {
            var arrival = Context.Flight.PositionUpdates
                .Where(q => q.Heading != 0)
                .OrderByDescending(q => q.TimeStamp)
                .Take(5)
                .ToList();

            if (!arrival.Any()) return;

            Context.Flight.ArrivalInfoFound = true;
            Context.Flight.ArrivalHeading = Convert.ToInt16(arrival.Average(q => q.Heading));
            Context.Flight.ArrivalLocation = arrival.First().GeoCoordinate;
            
            Context.InvokeOnLandingEvent();
        }
    }
}
