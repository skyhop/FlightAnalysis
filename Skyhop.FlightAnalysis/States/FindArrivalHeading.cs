using System;
using System.Linq;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        internal static void FindArrivalHeading(this FlightContext context)
        {
            var arrival = context.Flight.PositionUpdates
                .Where(q => q.Heading != 0 && !double.IsNaN(q.Heading))
                .OrderByDescending(q => q.TimeStamp)
                .Take(5)
                .ToList();

            if (!arrival.Any()) return;

            context.Flight.ArrivalInfoFound = true;
            context.Flight.ArrivalHeading = Convert.ToInt16(arrival.Average(q => q.Heading));
            context.Flight.ArrivalLocation = arrival.First().Location;

            if (context.Flight.ArrivalHeading == 0) context.Flight.ArrivalHeading = 360;

            context.InvokeOnLandingEvent();
        }
    }
}
