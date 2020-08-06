using System.Linq;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        internal static void DetermineFlightState(this FlightContext context)
        {
            var positionUpdate = context.Flight.PositionUpdates.LastOrDefault();

            // Flight has ended when the speed = 0 and the heading = 0
            if (positionUpdate.Speed == 0)
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

                // We might as well check for Context.Flight.DepartureInfoFound != null, I think
                if (context.Flight.StartTime != null ||
                    context.Flight.DepartureInfoFound == false)
                {
                    context.Flight.EndTime = positionUpdate.TimeStamp;
                    context.StateMachine.Fire(FlightContext.Trigger.ResolveArrival);
                }

                context.StateMachine.Fire(FlightContext.Trigger.Next);
                return;
            }

            // This part is about the departure
            if (context.Flight.StartTime == null
                && positionUpdate.Speed > 30
                && context.Flight.DepartureInfoFound != false)
            {
                // We have to start the flight

                // Walk back to when the speed was 0
                var start = context.Flight.PositionUpdates.Where(q => q.TimeStamp < positionUpdate.TimeStamp && (q.Speed == 0 || double.IsNaN(q.Speed)))
                    .OrderByDescending(q => q.TimeStamp)
                    .FirstOrDefault();

                if (start == null)
                {
                    // This means that an aircraft is flying but the takeoff itself hasn't been recorded due to insufficient flarm coverage
                    context.Flight.DepartureInfoFound = false;
                    context.InvokeOnRadarContactEvent();
                }
                else
                {
                    context.Flight.DepartureInfoFound = true;
                    context.Flight.StartTime = start.TimeStamp;

                    // Remove the points we do not need. (From before the flight, for example during taxi)
                    context.Flight.PositionUpdates
                        .Where(q => q.TimeStamp < context.Flight.StartTime.Value)
                        .ToList()
                        .ForEach(q => context.Flight.PositionUpdates.Remove(q));
                }
            }

            if (context.Flight.StartTime != null
                && context.Flight.DepartureHeading == 0)
            {
                context.StateMachine.Fire(FlightContext.Trigger.ResolveDeparture);
            }


            if (context.Flight.DepartureInfoFound == true
                && context.Flight.LaunchFinished == null)
            {
                context.StateMachine.Fire(FlightContext.Trigger.ResolveLaunchMethod);
            }

            context.StateMachine.Fire(FlightContext.Trigger.Next);
        }
    }
}
