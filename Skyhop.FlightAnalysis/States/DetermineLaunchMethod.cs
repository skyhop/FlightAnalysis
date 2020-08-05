using Skyhop.FlightAnalysis.Internal;
using Skyhop.FlightAnalysis.Models;
using System.Collections.Generic;
using System.Linq;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {

        internal static void DetermineLaunchMethod(this FlightContext context)
        {
            /*
             * The launch method will be determined after the departure direction has been confirmed.
             * 
             * After this we'll do an elimination to find the launch method. Preffered order;
             * 
             * 1. Winch launch
             * 2. Aerotow
             * 3. Self launch
             * 
             * First we'll check whether we're dealing with a winch launch. In order to qualify for a winch launch;
             * - Average heading deviation no more than 15°
             * - Climb distance less than 2 kilometers
             * - Winch distance per meter altitude between 0.2 and 0.8?
             */

            var climbrate = new List<double>(context.Flight.PositionUpdates.Count);

            for (var i = 1; i < context.Flight.PositionUpdates.Count; i++)
            {
                var deltaTime = context.Flight.PositionUpdates[i].TimeStamp - context.Flight.PositionUpdates[i - 1].TimeStamp;
                var deltaAltitude = context.Flight.PositionUpdates[i].Altitude - context.Flight.PositionUpdates[i - 1].Altitude;

                climbrate.Add(deltaAltitude / deltaTime.TotalMinutes);
            }

            if (climbrate.Count < 21) return;

            var result = ZScore.StartAlgo(climbrate, 20, 2, 0.7);

            if (result.Signals.Last() == -1)
            {
                // ToDo: Determine the launchMethod

                // Check the average heading and any deviation
                // Check the length

                var averageHeading = context.Flight.PositionUpdates.Average(q => q.Heading);

                // Skip the first element because heading is 0 when in rest
                var headingError = context.Flight.PositionUpdates
                    .Skip(1)
                    .Select(q => Geo.GetHeadingError(averageHeading, q.Heading))
                    .ToList();

                // Just assume we're screwing this winch launch over.
                if (headingError.Any(q => q > 20))
                {
                    context.Flight.LaunchMethod = LaunchMethod.Self;
                    context.InvokeOnLaunchCompletedEvent();
                }

                context.Flight.LaunchMethod = LaunchMethod.Winch;
                context.InvokeOnLaunchCompletedEvent();
            }
        }
    }
}
