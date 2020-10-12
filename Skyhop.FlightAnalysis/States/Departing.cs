using Skyhop.FlightAnalysis.Internal;
using Skyhop.FlightAnalysis.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        internal static void Departing(this FlightContext context)
        {
            /*
             * First check plausible scenarios. The easiest to track is an aerotow.
             * 
             * If not, wait until the launch is completed.
             */

            if (context.Flight.LaunchMethod == LaunchMethods.None)
            {
                var departure = context.Flight.PositionUpdates
                    .Where(q => q.Heading != 0 && !double.IsNaN(q.Heading))
                    .OrderBy(q => q.TimeStamp)
                    .Take(5)
                    .ToList();

                if (departure.Count > 4)
                {
                    context.Flight.DepartureHeading = Convert.ToInt16(departure.Average(q => q.Heading));
                    context.Flight.DepartureLocation = departure.First().Location;

                    if (context.Flight.DepartureHeading == 0) context.Flight.DepartureHeading = 360;

                    // Only start method recognition after the heading has been determined
                    context.Flight.LaunchMethod = LaunchMethods.Unknown | LaunchMethods.Aerotow | LaunchMethods.Winch | LaunchMethods.Self;
                }
                else return;
            }

            if (context.Flight.DepartureTime != null &&
                (context.CurrentPosition.TimeStamp - (context.Flight.PositionUpdates.FirstOrDefault(q => q.Speed > 30)?.TimeStamp ?? context.CurrentPosition.TimeStamp)).TotalSeconds < 20) return;

            // We can safely try to extract the correct path


            if (context.Flight.LaunchMethod.HasFlag(LaunchMethods.Unknown | LaunchMethods.Aerotow))
            {
                var isTow = context.IsAerotow();

                if (isTow == null)
                {
                    context.Flight.LaunchMethod &= ~LaunchMethods.Aerotow;
                }
                else 
                {
                    if (isTow.Value.status == Geo.AircraftRelation.OnTow)
                    {
                        context.Flight.LaunchMethod = LaunchMethods.Aerotow | LaunchMethods.OnTow;
                        context.Flight.Encounters.Add(new Encounter
                        {
                            Aircraft = isTow.Value.context.Options.AircraftId,
                            Start = isTow.Value.context.Flight.DepartureTime,
                            Type = EncounterType.Tug
                        });

                        context.StateMachine.Fire(FlightContext.Trigger.TrackAerotow);
                    }
                    else if (isTow.Value.status == Geo.AircraftRelation.Towplane)
                    {
                        context.Flight.LaunchMethod = LaunchMethods.Aerotow | LaunchMethods.TowPlane;
                        context.Flight.Encounters.Add(new Encounter
                        {
                            Aircraft = isTow.Value.context.Options.AircraftId,
                            Start = isTow.Value.context.Flight.DepartureTime,
                            Type = EncounterType.Tow
                        });

                        context.StateMachine.Fire(FlightContext.Trigger.TrackAerotow);
                    }
                }

                return;
            }

            // Hardwire a check to see if we're sinking again to abort the departure, but only if we're not behind a tow.
            if (!context.Flight.LaunchMethod.HasFlag(LaunchMethods.Aerotow)
                && context.Flight.PositionUpdates.Last().Altitude - context.CurrentPosition.Altitude > 3)
            {
                context.StateMachine.Fire(FlightContext.Trigger.Landing);
                return;
            }

            if (context.Flight.LaunchMethod.HasFlag(LaunchMethods.Unknown | LaunchMethods.Winch))
            {
                // ToDo: Check whether the launch has been completed

                var climbrate = new List<double>(context.Flight.PositionUpdates.Count);

                for (var i = 1; i < context.Flight.PositionUpdates.Count; i++)
                {
                    var deltaTime = context.Flight.PositionUpdates[i].TimeStamp - context.Flight.PositionUpdates[i - 1].TimeStamp;
                    var deltaAltitude = context.Flight.PositionUpdates[i].Altitude - context.Flight.PositionUpdates[i - 1].Altitude;

                    climbrate.Add(deltaAltitude / deltaTime.TotalMinutes);
                }

                //if (climbrate.Count < 21) return;

                var result = ZScore.StartAlgo(climbrate, context.Flight.PositionUpdates.Count / 3, 2, 0.7);

                // When the initial climb has completed
                if (result.Signals.Any(q => q == -1))
                {
                    var averageHeading = context.Flight.PositionUpdates.Average(q => q.Heading);

                    // ToDo: Add check to see whether there is another aircraft nearby
                    if (context.Flight.PositionUpdates
                            .Skip(1)        // Skip the first element because heading is 0 when in rest
                            .Select(q => Geo.GetHeadingError(averageHeading, q.Heading))
                            .Any(q => q > 20)
                        || (context.Options.NearbyAircraftAccessor?.Invoke(context.CurrentPosition.Location, 0.2).Any() ?? false)
                        || Geo.DistanceTo(
                            context.Flight.PositionUpdates.First().Location,
                            context.CurrentPosition.Location) > 3000)
                    {
                        context.Flight.LaunchMethod &= ~LaunchMethods.Winch;
                    }
                    else
                    {
                        context.Flight.LaunchFinished = context.CurrentPosition.TimeStamp;
                        context.Flight.LaunchMethod = LaunchMethods.Winch;
                        context.InvokeOnLaunchCompletedEvent();
                        context.StateMachine.Fire(FlightContext.Trigger.LaunchCompleted);
                    }
                }
            }
            else if (context.Flight.LaunchMethod.HasFlag(LaunchMethods.Unknown | LaunchMethods.Self))
            {
                context.Flight.LaunchFinished = context.CurrentPosition.TimeStamp;
                context.Flight.LaunchMethod = LaunchMethods.Self;
                context.InvokeOnLaunchCompletedEvent();
                context.StateMachine.Fire(FlightContext.Trigger.LaunchCompleted);
            }
        }
    }
}
