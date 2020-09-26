using Skyhop.FlightAnalysis.Internal;
using System;
using System.Linq;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        internal static void ProcessNextPoint(this FlightContext context)
        {
            if (context.Flight == null || context.Flight.EndTime != null)
            {
                // Reset the context
                context.StateMachine.Fire(FlightContext.Trigger.Initialize);

                return;
            }

            if (!context.PriorityQueue.Any())
            {
                context.StateMachine.Fire(FlightContext.Trigger.Standby);
                return;
            }

            var position = context.PriorityQueue.Dequeue();

            if (position == null
                || context.Flight.PositionUpdates
                    .TakeLast(5)
                    .Any(q => q?.Speed > 10
                        && q?.Latitude == position.Latitude
                        && q?.Longitude == position.Longitude))
            {
                context.StateMachine.Fire(FlightContext.Trigger.Next);
                return;
            }

            if (context.Flight.PositionUpdates.Any())
            {
                var deltaTime = position.TimeStamp - context.Flight.PositionUpdates.Last().TimeStamp;
                if (deltaTime.TotalSeconds < 0.1)
                {
                    context.StateMachine.Fire(FlightContext.Trigger.Next);
                    return;
                }
            }

            // ToDo: Put this part in a state step
            position = Geo.NormalizeData(context, position);
            
            if (position == null)
            {
                context.StateMachine.Fire(FlightContext.Trigger.Next);
                return;
            }

            context.Flight.PositionUpdates.Add(position);
            context.CleanupDataPoints();

            if (double.IsNaN(position.Heading)
                || double.IsNaN(position.Speed))
            {
                context.StateMachine.Fire(FlightContext.Trigger.Next);
                return;
            }

            if (context.LatestTimeStamp == DateTime.MinValue) context.LatestTimeStamp = position.TimeStamp;

            if (context.Flight.StartTime == null)
            {
                // Just keep the buffer small by removing points older then 2 minutes. The flight hasn't started anyway
                context.Flight.PositionUpdates
                    .Where(q => q.TimeStamp < position.TimeStamp.AddMinutes(-2))
                    .ToList()
                    .ForEach(q => context.Flight.PositionUpdates.Remove(q));
            }
            else if (context.LatestTimeStamp < position.TimeStamp.AddHours(-8))
            {
                context.InvokeOnCompletedWithErrorsEvent();

                context.StateMachine.Fire(FlightContext.Trigger.Initialize);

                context.LatestTimeStamp = position.TimeStamp;
                return;
            }

            context.LatestTimeStamp = position.TimeStamp;

            context.StateMachine.Fire(FlightContext.Trigger.ResolveState);
        }
    }
}
