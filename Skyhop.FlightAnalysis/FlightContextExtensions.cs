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
        internal static IEnumerable<Encounter> TowEncounter(this FlightContext context)
        {
            var nearbyAircraft = context.Options.NearbyAircraftAccessor?.Invoke(
                context.CurrentPosition.Location,
                0.5)
                .ToList();

            if (nearbyAircraft == null) yield break;

            foreach (var aircraft in nearbyAircraft)
            {
                var iAm = context.WhatAmI(aircraft);

                if (iAm == AircraftRelation.OnTow)
                {
                    yield return new Encounter
                    {
                        Aircraft = aircraft.Options.AircraftId,
                        Start = aircraft.Flight.DepartureTime,
                        Type = EncounterType.Tug
                    };
                }
                else if (iAm == AircraftRelation.Towplane)
                {
                    yield return new Encounter
                    {
                        Aircraft = aircraft.Options.AircraftId,
                        Start = aircraft.Flight.DepartureTime,
                        Type = EncounterType.Tow
                    };
                }
            }
        }

        internal static AircraftRelation WhatAmI(this FlightContext context1, FlightContext context2)
        {
            var c2Position = context2.GetPositionAt(context1.CurrentPosition.TimeStamp);

            // In this case we conclusively know there's nothing to be found
            if (c2Position == null
                || context2.CurrentPosition == null
                || (context1.CurrentPosition.TimeStamp - context2.CurrentPosition.TimeStamp).TotalSeconds > 30
                || context1.CurrentPosition.Location.DistanceTo(c2Position.Location) > 200)
            {
                return AircraftRelation.None;
            }
            
            // Calculate the average bearing to remove uncertainty
            var bearings = new List<double>();

            for (var i = context1.Flight.PositionUpdates.Count - 1; i > 0; i--)
            {
                var p1 = context1.Flight.PositionUpdates[i];
                var p2 = context2.GetPositionAt(p1.TimeStamp);

                if (Math.Abs(p1.Speed - p2.Speed) > 10
                    || Math.Abs(p1.Altitude - p2.Altitude) > 100
                    || p1.Location.DistanceTo(p2.Location) > 200) return AircraftRelation.None;

                bearings.Add(p1.Location.DegreeBearing(p2.Location));
            }

            var bearing = bearings.Average();

            return 90 < bearing && bearing < 270
                ? AircraftRelation.Towplane
                : AircraftRelation.OnTow;
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
