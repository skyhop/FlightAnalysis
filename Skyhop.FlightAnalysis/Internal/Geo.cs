﻿using System;
using System.Linq;
using NetTopologySuite.Geometries;
using Skyhop.FlightAnalysis.Models;

namespace Skyhop.FlightAnalysis.Internal
{
    internal static class Geo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="coordinate1"></param>
        /// <param name="coordinate2"></param>
        /// <returns></returns>
        /// <remarks>See https://stackoverflow.com/a/2042883/1720761 for more information about this method.</remarks>
        internal static double DegreeBearing(
            Point coordinate1,
            Point coordinate2)
        {
            var dLon = ToRad(coordinate2.Y - coordinate1.Y);
            var dPhi = Math.Log(
                Math.Tan(ToRad(coordinate2.X) / 2 + Math.PI / 4) / Math.Tan(ToRad(coordinate1.X) / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : 2 * Math.PI + dLon;
            return ToBearing(Math.Atan2(dLon, dPhi));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        /// <remarks>See https://stackoverflow.com/a/2042883/1720761 for more information about this method.</remarks>
        internal static double ToRad(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        internal static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        /// <summary>
        /// Convert radians to degrees (as bearing: 0...360)
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        /// <remarks>See https://stackoverflow.com/a/2042883/1720761 for more information about this method.</remarks>
        internal static double ToBearing(double radians)
        {
            return (ToDegrees(radians) + 360) % 360;
        }

        /// <summary>
        ///     Returns the distance between the latitude and longitude coordinates that are specified by this GeoCoordinate and
        ///     another specified GeoCoordinate.
        /// </summary>
        /// <returns>
        ///     The distance between the two coordinates, in meters.
        /// </returns>
        /// <param name="other">The GeoCoordinate for the location to calculate the distance to.</param>
        internal static double DistanceTo(this Point from, Point to)
        {
            if (double.IsNaN(from.X) || double.IsNaN(from.Y)
               || double.IsNaN(to.X) || double.IsNaN(to.Y))
            {
                throw new ArgumentException("Argument latitude or longitude is not a number");
            }

            var d1 = from.X * (Math.PI / 180.0);
            var num1 = from.Y * (Math.PI / 180.0);
            var d2 = to.X * (Math.PI / 180.0);
            var num2 = to.Y * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                     Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }

        internal static double GetHeadingError(double initial, double final)
        {
            var diff = final - initial;
            var absDiff = Math.Abs(diff);

            if (absDiff <= 180)
            {
                return absDiff == 180 ? absDiff : diff;
            }
            else if (final > initial)
            {
                return absDiff - 360;
            }
            else
            {
                return 360 - absDiff;
            }
        }

        // ToDo: Add information about the aircrafts climbrate and so on, if possible
        internal static PositionUpdate NormalizeData(FlightContext context, PositionUpdate position)
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
