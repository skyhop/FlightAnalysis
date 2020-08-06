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
             * First check plausible scenarios. The easiest to track is an aerotow.
             * 
             * If not, wait until the launch is completed.
             */

            if (context.Flight.LaunchMethod == LaunchMethods.None)
            {
                context.Flight.LaunchMethod = LaunchMethods.Unknown | LaunchMethods.Aerotow | LaunchMethods.Winch | LaunchMethods.Self;
            }
            
            if (context.Flight.LaunchMethod.HasFlag(LaunchMethods.Unknown | LaunchMethods.Aerotow))
            {
                var isTow = context.IsAerotow();

                if (isTow == null) context.Flight.LaunchMethod &= ~LaunchMethods.Aerotow;
                else context.Flight.LaunchMethod = LaunchMethods.Aerotow;
            }

            if (context.Flight.LaunchMethod.HasFlag(LaunchMethods.Unknown))
            {
                // ToDo: Check whether the launch has been completed

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
                    context.Flight.LaunchFinished = context.Flight.PositionUpdates.Last().TimeStamp;

                    // ToDo: Determine the launch method
                    var averageHeading = context.Flight.PositionUpdates.Average(q => q.Heading);

                    // Skip the first element because heading is 0 when in rest
                    var headingError = context.Flight.PositionUpdates
                        .Skip(1)
                        .Select(q => Geo.GetHeadingError(averageHeading, q.Heading))
                        .ToList();

                    // Just assume we're screwing this winch launch over.
                    if (headingError.Any(q => q > 20)
                        || Geo.DistanceTo(
                            context.Flight.PositionUpdates.First().Location,
                            context.Flight.PositionUpdates.Last().Location) < 3000)
                    {
                        context.Flight.LaunchMethod = LaunchMethods.Self;
                        context.InvokeOnLaunchCompletedEvent();
                    }
                    else
                    {
                        context.Flight.LaunchMethod = LaunchMethods.Winch;
                        context.InvokeOnLaunchCompletedEvent();
                    }
                }
            }
        }
    }
}
