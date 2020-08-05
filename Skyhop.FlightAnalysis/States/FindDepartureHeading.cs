using System;
using System.Linq;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        internal static void FindDepartureHeading(this FlightContext context)
        {
            var departure = context.Flight.PositionUpdates
                .Where(q => q.Heading != 0 && !double.IsNaN(q.Heading))
                .OrderBy(q => q.TimeStamp)
                .Take(5)
                .ToList();

            if (departure.Count < 5) return;

            context.Flight.DepartureHeading = Convert.ToInt16(departure.Average(q => q.Heading));
            context.Flight.DepartureLocation = departure.First().Location;

            if (context.Flight.DepartureHeading == 0) context.Flight.DepartureHeading = 360;

            context.InvokeOnTakeoffEvent();
        }
    }
}
