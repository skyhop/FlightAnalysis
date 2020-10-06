using System;
using System.Collections.Generic;
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

            if (context.ArrivalTheory != null)
            {
                context.ArrivalTheory.Cancel();
                context.ArrivalTheory = null;
            }

            if (context.CurrentPosition.Altitude > Constants.ArrivalHeight)
            {
                context.StateMachine.Fire(FlightContext.Trigger.LandingAborted);
                return;
            }

            var arrival = context.Flight.PositionUpdates
                    .Where(q => q.Heading != 0 && !double.IsNaN(q.Heading))
                    .OrderByDescending(q => q.TimeStamp)
                    .Take(5)
                    .ToList();

            if (!arrival.Any()) return;


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
                context.Flight.ArrivalInfoFound = true;
                context.Flight.ArrivalHeading = Convert.ToInt16(arrival.Average(q => q.Heading));
                context.Flight.ArrivalLocation = arrival.First().Location;

                if (context.Flight.ArrivalHeading == 0) context.Flight.ArrivalHeading = 360;

                context.InvokeOnLandingEvent();

                /*
                 * In order to prevent the machine from reusing a totally irrelevant data point, remove this one 
                 * to force the machine to collect new data, or to estimate a reasonable departure time.
                 */

                context.CurrentPosition = null;
                context.StateMachine.Fire(FlightContext.Trigger.Arrived);
            }
            else if (!(context.Flight.ArrivalInfoFound ?? true)
                && context.CurrentPosition.TimeStamp > context.Flight.EndTime.Value.AddSeconds(Constants.ArrivalTimeout))
            {
                // Our theory needs to be finalized
                context.InvokeOnLandingEvent();

                context.StateMachine.Fire(FlightContext.Trigger.Arrived);
            }
            else
            {
                var previousPoint = context.Flight.PositionUpdates.LastOrDefault();

                if (previousPoint == null) return;

                // Take the average climbrate over the last few points

                var climbrates = new List<double>();

                for (var i = context.Flight.PositionUpdates.Count - 1; i > Math.Max(context.Flight.PositionUpdates.Count - 15, 0); i--)
                {
                    var p1 = context.Flight.PositionUpdates[i];
                    var p2 = context.Flight.PositionUpdates[i - 1];

                    var deltaAltitude = p1.Altitude - p2.Altitude;
                    var deltaTime = p1.TimeStamp - p2.TimeStamp;

                    climbrates.Add(deltaAltitude / deltaTime.TotalSeconds);
                }

                if (!climbrates.Any())
                {
                    context.Flight.EndTime = null;
                    context.Flight.ArrivalInfoFound = null;
                    context.Flight.ArrivalHeading = 0;
                    return;
                }

                var average = climbrates.Average();

                double ETUA = context.CurrentPosition.Altitude / -average;

                if (double.IsInfinity(ETUA) || ETUA > (60 * 10) || ETUA < 0)
                {
                    context.Flight.EndTime = null;
                    context.Flight.ArrivalInfoFound = null;
                    context.Flight.ArrivalHeading = 0;
                    return;
                }

                context.Flight.EndTime = context.CurrentPosition.TimeStamp.AddSeconds(ETUA);
                context.Flight.ArrivalInfoFound = false;
                context.Flight.ArrivalHeading = Convert.ToInt16(arrival.Average(q => q.Heading));
            }
        }
    }
}
