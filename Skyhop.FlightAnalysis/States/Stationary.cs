using Skyhop.FlightAnalysis.Internal;
using System;
using System.Linq;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        internal static void Stationary(this FlightContext context)
        {
            if (context.CurrentPosition == null) return;

            if (context.CurrentPosition.Speed > 20 && context.Flight.PositionUpdates.Delta(q => q.Altitude).Any(q => Math.Abs(q) > 6))
            {
                // Walk back to when the speed was 0
                var start = context.Flight.PositionUpdates
                    .Where(q => (q.Speed == 0 || double.IsNaN(q.Speed))
                        && (context.CurrentPosition.TimeStamp - q.TimeStamp).TotalSeconds < 60)
                    .OrderByDescending(q => q.TimeStamp)
                    .FirstOrDefault();

                double groundElevation = start?.Altitude ?? 0;
                if (context.Options.NearbyRunwayAccessor != null)
                {
                    groundElevation = context.Options.NearbyRunwayAccessor(
                        context.CurrentPosition.Location,
                        Constants.RunwayQueryRadius)?
                        .OrderBy(q => q.Sides
                            .Min(w => Geo.DistanceTo(w, context.CurrentPosition.Location))
                        ).FirstOrDefault()
                        ?.Sides
                        .Average(q => q.Z)
                        ?? 0;
                }

                if (start == null && context.CurrentPosition.Altitude > (groundElevation + Constants.ArrivalHeight))
                {
                    // The flight was already in progress, or we could not find the starting point (trees in line of sight?)

                    // Create an estimation about the departure time. Unless contact happens high in the sky
                    context.Flight.DepartureInfoFound = false;

                    context.InvokeOnRadarContactEvent();

                    context.StateMachine.Fire(FlightContext.Trigger.TrackMovements);
                    return;
                }
                else if (start == null && context.CurrentPosition.Altitude <= (groundElevation + Constants.ArrivalHeight))
                {
                    // ToDo: Try to estimate the departure time
                    context.Flight.DepartureTime = context.CurrentPosition.TimeStamp;
                    context.Flight.DepartureLocation = context.CurrentPosition.Location;

                    context.Flight.PositionUpdates
                        .Where(q => q.TimeStamp < context.Flight.DepartureTime.Value)
                        .ToList()
                        .ForEach(q => context.Flight.PositionUpdates.Remove(q));

                    context.Flight.DepartureInfoFound = false;
                }
                else if (start != null)
                {
                    context.Flight.DepartureTime = start.TimeStamp;
                    context.Flight.DepartureLocation = start.Location;

                    // Remove points not related to this flight
                    context.Flight.PositionUpdates
                        .Where(q => q.TimeStamp < context.Flight.DepartureTime.Value)
                        .ToList()
                        .ForEach(q => context.Flight.PositionUpdates.Remove(q));

                    context.Flight.DepartureInfoFound = false;
                }

                context.Flight.DepartureHeading = (short)context.CurrentPosition.Heading;
                context.InvokeOnTakeoffEvent();

                context.StateMachine.Fire(FlightContext.Trigger.Depart);
            }
        }
    }
}
