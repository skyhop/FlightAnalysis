using System;
using System.Linq;
using System.Threading.Tasks;

namespace Boerman.FlightAnalysis.FlightStates
{
    /// <summary>
    /// The FindDepartureHeading state is being invoked once takeoff had been detected. Sole purpose of this class is 
    /// to determine the heading during takeoff. This data can later on be used to determine the departure runway.
    /// </summary>
    internal class FindDepartureHeading : FlightState
    {
        public FindDepartureHeading(FlightContext context) : base(context)
        {
        }
        
        public override async Task Run()
        {
            var departure = Context.Flight.PositionUpdates
                .Where(q => q.Heading != 0 && !Double.IsNaN(q.Heading))
                .OrderBy(q => q.TimeStamp)
                .Take(5)
                .ToList();

            if (departure.Count() < 5) return;
            
            Context.Flight.DepartureHeading = Convert.ToInt16(departure.Average(q => q.Heading));
            Context.Flight.DepartureLocation = departure.First().Location;

            if (Context.Flight.DepartureHeading == 0) Context.Flight.DepartureHeading = 360;
            
            Context.InvokeOnTakeoffEvent();
        }
    }
}
