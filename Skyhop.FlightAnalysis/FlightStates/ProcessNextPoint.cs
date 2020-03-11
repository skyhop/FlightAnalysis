using System;
using System.Linq;
using System.Threading.Tasks;
using Skyhop.FlightAnalysis.Internal;
using Skyhop.FlightAnalysis.Models;

namespace Skyhop.FlightAnalysis.FlightStates
{
    /// <summary>
    /// The ProcessNextPoint state is being invoked to start processing of the next available data point.
    /// </summary>
    internal class ProcessNextPoint : FlightState
    {

        public ProcessNextPoint(FlightContext context) : base(context)
        {
        }

        public override async Task Run()
        {
            if (Context.Flight.EndTime != null)
            {
                Context.QueueState(typeof(InitializeFlightState));
                Context.QueueState(typeof(ProcessNextPoint));
            }
            else
            {
                if (!Context.PriorityQueue.Any()) return;

                var positionUpdate = NormalizeData(Context.PriorityQueue.Dequeue());

                if (positionUpdate != null)
                {
                    Context.Flight.PositionUpdates.Add(positionUpdate);
                    Context.CleanupDataPoints();
                }

                if (positionUpdate == null
                    || double.IsNaN(positionUpdate.Heading)
                    || double.IsNaN(positionUpdate.Speed))
                {
                    Context.QueueState(typeof(ProcessNextPoint));
                    return;
                }

                // Do a few checks to see whether the timing of positionupdates allows further processing.
                if (TimingChecks(positionUpdate.TimeStamp)) Context.QueueState(typeof(DetermineFlightState));
            }
        }

        // ToDo: Make the time periods used in this function configurable from the options class. (Context.Options)
        // ToDo: As this function is only used once, integrate it with the main flow for readability. (It's not completely clear right now where the queued states come from)
        private bool TimingChecks(DateTime currentTimeStamp)
        {
            if (Context.LatestTimeStamp == DateTime.MinValue) Context.LatestTimeStamp = currentTimeStamp;

            if (Context.Flight.StartTime == null)
            {
                // Just keep the buffer small by removing points older then 2 minutes. The flight hasn't started anyway
                Context.Flight.PositionUpdates
                        .Where(q => q.TimeStamp < currentTimeStamp.AddMinutes(-2))
                        .ToList()
                        .ForEach(q => Context.Flight.PositionUpdates.Remove(q));
            }
            else if (Context.LatestTimeStamp < currentTimeStamp.AddHours(-8))
            {
                Context.InvokeOnCompletedWithErrorsEvent();
                Context.QueueState(typeof(InitializeFlightState));
                Context.QueueState(typeof(ProcessNextPoint));
                Context.LatestTimeStamp = currentTimeStamp;
                return false;
            }

            Context.LatestTimeStamp = currentTimeStamp;
            return true;
        }

        // ToDo: Add information about the aircrafts climbrate and so on, if possible
        private PositionUpdate NormalizeData(PositionUpdate position)
        {
            if (position == null) return position;

            if (Context.Flight.PositionUpdates.Count < 2
                || !double.IsNaN(position.Heading) && !double.IsNaN(position.Speed)) return position;

            var previousPosition =
                Context.Flight.PositionUpdates.LastOrDefault();

            if (previousPosition == null) return position;

            double? heading = null;
            double? speed = null;

            if (double.IsNaN(position.Heading)) heading = Geo.DegreeBearing(previousPosition.Location, position.Location);

            if (double.IsNaN(position.Speed))
            {
                // 1. Get the distance (meters)
                // 2. Calculate the time difference (seconds)
                // 3. Convert to knots (1.94384449 is a constant)
                var distance = previousPosition.Location.DistanceTo(position.Location);
                var timeDifference = (position.TimeStamp - previousPosition.TimeStamp).Milliseconds;

                if (timeDifference == 0) return null;

                if (distance != 0) speed = distance / (timeDifference / 1000) * 1.94384449;
            }

            position.Speed = speed ?? position.Speed;
            position.Heading = heading ?? position.Heading;

            return position;
        }
    }
}
