using Skyhop.FlightAnalysis.Internal;
using Skyhop.FlightAnalysis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static Skyhop.FlightAnalysis.Internal.Geo;

namespace Skyhop.FlightAnalysis
{
    public static class FlightContextExtensions
    {
        internal static (FlightContext context, AircraftRelation status)? IsAerotow(this FlightContext context)
        {
            var nearbyAircraft = context.Options.NearbyAircraftAccessor?.Invoke(
                context.CurrentPosition.Location,
                0.5)
                .ToList();

            if (nearbyAircraft != null && nearbyAircraft.Count > 0)
            {
                if (nearbyAircraft.Any(q => q.Flight.DepartureTime == null))
                {
                    // We'll have to wait until those are departed.
                    return (null, AircraftRelation.None);
                }

                foreach (var aircraft in nearbyAircraft)
                {
                    var status = context.DetermineTowStatus(aircraft);

                    if (status == null) return (null, AircraftRelation.None);

                    if (status != AircraftRelation.None)
                    {
                        return (aircraft, status.Value);
                    }
                }
            }

            return null;
        }

        internal static AircraftRelation? DetermineTowStatus(this FlightContext context1, FlightContext context2)
        {
            var c2Position = context2.GetPositionAt(context1.CurrentPosition.TimeStamp);

            if (c2Position == null) return null;

            if (context1.CurrentPosition.Location.DistanceTo(c2Position.Location) > 200)
            {
                return AircraftRelation.None;
            }

            // Calculate the average bearing to remove uncertainty
            var bearings = new List<double>();

            for (var i = context1.Flight.PositionUpdates.Count - 1; i > 0; i--)
            {
                var p1 = context1.Flight.PositionUpdates[i];
                var p2 = context2.GetPositionAt(p1.TimeStamp);

                bearings.Add(p1.Location.DegreeBearing(p2.Location));
            }

            var bearing = bearings.Average();

            return 90 < bearing && bearing < 270
                ? AircraftRelation.OnTow
                : AircraftRelation.Towplane;
        }

        internal static AircraftRelation TrackTow(this FlightContext context1, FlightContext context2)
        {
            if (context1.Flight.PositionUpdates.Count < 5 || context2.Flight.PositionUpdates.Count < 5) return AircraftRelation.None;

            var interpolation = Interpolation.Interpolate(
                context1.Flight.PositionUpdates,
                context2.Flight.PositionUpdates,
                q => q.TimeStamp.Ticks,
                (object1, object2, time) =>
                {
                    var dX = object2.Location.X - object1.Location.X;
                    var dY = object2.Location.Y - object1.Location.Y;
                    var dT = (object2.TimeStamp - object1.TimeStamp).Ticks;

                    if (dT == 0) return null;

                    double factor = (time - object1.TimeStamp.Ticks) / (double)dT;

                    return new PositionUpdate(
                        "",
                        new DateTime((long)(object1.TimeStamp.Ticks + dT * factor)),
                        object1.Location.Y + factor * dY,
                        object1.Location.X + factor * dX);
                });

            /*
             * We only care about the following two characteristics;
             * 1. In relation to the glider, the towplane is always in front -90 to +90 bearing
             * 2. The aircraft should be no more than 200 meters apart for any interpolated point
             * 
             * As soon as any of these is false, the tow has ended.
             */

            bool? isTowed = null;

            foreach (var dataPoint in interpolation)
            {
                if (dataPoint.T1.Location.DistanceTo(dataPoint.T2.Location) > 200)
                {
                    break;
                }

                // Determine the position irt to the other aircraft
                var bearing = dataPoint.T1.Location.DegreeBearing(dataPoint.T2.Location);

                if (90 < bearing && bearing < 270)
                {
                    // We're being towed
                    if (isTowed == null) isTowed = true;
                    else if (isTowed == false) return AircraftRelation.None;
                }
                else
                {
                    // We're towing
                    if (isTowed == null) isTowed = false;
                    else if (isTowed == true) return AircraftRelation.None;
                }
            }

            if (isTowed == null) return AircraftRelation.None;

            return isTowed.Value
                ? AircraftRelation.OnTow
                : AircraftRelation.Towplane;
        }
    }
}
