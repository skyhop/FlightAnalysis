using System;
using System.Linq;
using System.Threading.Tasks;

namespace Boerman.FlightAnalysis.FlightStates
{
    /// <summary>
    /// The DetermineFlightState class contains the main logic to check whether a specific aircraft is flying or not
    /// </summary>
    internal class DetermineFlightState : FlightState
    {
        public DetermineFlightState(FlightContext context) : base(context)
        {
        }

        public override async Task Run()
        {
            var positionUpdate = Context.Flight.PositionUpdates.LastOrDefault();

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
                if (Context.Flight.StartTime != null ||
                   Context.Flight.DepartureInfoFound == false)
                {
                    Context.Flight.EndTime = positionUpdate.TimeStamp;
                    Context.QueueState(typeof(FindArrivalHeading));
                }
            }
            else
            {
                // This part is about the departure
                if (Context.Flight.StartTime == null 
                    && positionUpdate.Speed > 30
                    && Context.Flight.DepartureInfoFound != false)
                {
                    // We have to start the flight

                    // Walk back to when the speed was 0
                    var start = Context.Flight.PositionUpdates.Where(q => q.TimeStamp < positionUpdate.TimeStamp && (q.Speed == 0 || Double.IsNaN(q.Speed)))
                        .OrderByDescending(q => q.TimeStamp)
                        .FirstOrDefault();
                    
                    if (start == null)
                    {
                        // This means that an aircraft is flying but the takeoff itself hasn't been recorded due to insufficient flarm coverage
                        Context.Flight.DepartureInfoFound = false;
                        Context.InvokeOnRadarContactEvent();
                    }
                    else
                    {
                        Context.Flight.DepartureInfoFound = true;
                        Context.Flight.StartTime = start.TimeStamp;

                        // Remove the points we do not need. (From before the flight, for example during taxi)
                        Context.Flight.PositionUpdates
                            .Where(q => q.TimeStamp < Context.Flight.StartTime.Value)
                            .ToList()
                            .ForEach(q => Context.Flight.PositionUpdates.Remove(q));
                    }
                }

                if (Context.Flight.StartTime != null 
                    && Context.Flight.DepartureHeading == 0)
                {
                    Context.QueueState(typeof(FindDepartureHeading));
                }
            }

            Context.QueueState(typeof(ProcessNextPoint));
        }
    }
}
