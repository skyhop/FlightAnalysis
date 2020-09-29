using System;
using System.Linq;

namespace Skyhop.FlightAnalysis.Experimental
{
    internal static partial class MachineStates
    {
        internal static void Arriving(this FlightContext context)
        {
            /*
             * - Create an estimate for the arrival time
             * - When data shows a landing, use that data
             * - When no data is received anymore, use the estimation
             */

            if (context.CurrentPosition.Speed == 0)
            {
                /*
                 * If a flight has been in progress, end the flight.
                 * 
                 * When the aircraft has been registered mid flight the departure
                 * location is unknown, and so is the time. Therefore look at the
                 * flag which is set to indicate whether the departure location has
                 * been found.
                 * 
                 * ToDo: Also check the vertical speed as it might be an indication
                 * that the flight is still in progress! (Aerobatic stuff and so)
                 */

                context.Flight.EndTime = context.CurrentPosition.TimeStamp;

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
}
