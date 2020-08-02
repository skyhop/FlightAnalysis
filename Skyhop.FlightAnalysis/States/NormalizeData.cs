using Skyhop.FlightAnalysis.Internal;
using Skyhop.FlightAnalysis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Skyhop.FlightAnalysis.States
{
    public static partial class States
    {
        // ToDo: Add information about the aircrafts climbrate and so on, if possible
        public static PositionUpdate NormalizeData(FlightContext context, PositionUpdate position)
        {
            if (position == null) return position;

            if (context.Flight.PositionUpdates.Count < 2
                || !double.IsNaN(position.Heading) && !double.IsNaN(position.Speed)) return position;

            var previousPosition =
                context.Flight.PositionUpdates.LastOrDefault();

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
