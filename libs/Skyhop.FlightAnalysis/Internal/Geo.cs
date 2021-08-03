using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;
using Skyhop.FlightAnalysis.Models;
using UnitsNet;

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
            this Point coordinate1,
            Point coordinate2)
        {
            var dLon = ToRad(coordinate2.Y - coordinate1.Y);
            var dPhi = Math.Log(
                Math.Tan((ToRad(coordinate2.X) / 2) + (Math.PI / 4)) / Math.Tan((ToRad(coordinate1.X) / 2) + (Math.PI / 4)));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -((2 * Math.PI) - dLon) : (2 * Math.PI) + dLon;
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

        // Source: https://rosettacode.org/wiki/Averages/Mean_angle#C.23
        internal static double MeanAngle(this double[] angles)
        {
            var x = angles.Sum(a => Math.Cos(a * Math.PI / 180)) / angles.Length;
            var y = angles.Sum(a => Math.Sin(a * Math.PI / 180)) / angles.Length;
            return Math.Abs(Math.Atan2(y, x) * 180 / Math.PI);
        }

        /// <summary>
        ///     Returns the distance between the latitude and longitude coordinates that are specified by this GeoCoordinate and
        ///     another specified GeoCoordinate.
        /// </summary>
        /// <returns>
        ///     The distance between the two coordinates, in meters.
        /// </returns>
        /// <param name="from"></param>
        /// <param name="to"></param>
        internal static double DistanceTo(this Point from, Point to)
        {
            if (double.IsNaN(from.X) || double.IsNaN(from.Y)
               || double.IsNaN(to.X) || double.IsNaN(to.Y))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Argument latitude or longitude is not a number");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            var d1 = from.X * (Math.PI / 180.0);
            var num1 = from.Y * (Math.PI / 180.0);
            var d2 = to.X * (Math.PI / 180.0);
            var num2 = (to.Y * (Math.PI / 180.0)) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                     (Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0));

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
            if (position == null 
                || (!double.IsNaN(position.Heading) && !double.IsNaN(position.Speed))) return position;

            var previousPosition = context.CurrentPosition;

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
                double timeDifference = (position.TimeStamp - previousPosition.TimeStamp).TotalSeconds;

                if (timeDifference < 0.050) return position;

                if (distance != 0) speed = Speed.FromMetersPerSecond(distance / timeDifference).Knots;
                else speed = 0;
            }

            position.Speed = speed ?? position.Speed;
            position.Heading = heading ?? position.Heading;

            return position;
        }

        /// <summary>
        /// More info on http://www.movable-type.co.uk/scripts/latlong.html
        /// </summary>
        /// <param name="point">Starting point</param>
        /// <param name="bearing">Bearing in degrees</param>
        /// <param name="distance">Distance in kilometers</param>
        /// <returns></returns>
        internal static Point HaversineExtrapolation(this Point point, double bearing, double distance)
        {
            // The earth's radius in km
            const int R = 6371;

            var φ1 = point.Y;   // φ is the latitude
            var λ1 = point.X;   // λ is the longitude

            var φ2 = Math.Asin((Math.Sin(φ1) * Math.Cos(distance / R)) +
                      (Math.Cos(φ1) * Math.Sin(distance / R) * Math.Cos(bearing)));

            var λ2 = λ1 + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance / R) * Math.Cos(φ1),
                                       Math.Cos(distance / R) - (Math.Sin(φ1) * Math.Sin(φ2)));

            return new Point(λ2, φ2);
        }

        internal enum AircraftRelation
        {
            None,
            Indeterministic,
            Towplane,
            OnTow
        }
    }
}
