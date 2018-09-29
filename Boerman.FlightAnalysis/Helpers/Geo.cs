using GeoAPI.Geometries;
using System;

namespace Boerman.FlightAnalysis.Helpers
{
    public static class Geo
    {
        // See https://stackoverflow.com/a/2042883/1720761 for more information about these methods.

        public static double DegreeBearing(
            Coordinate coordinate1,
            Coordinate coordinate2)
        {
            var dLon = ToRad(coordinate2.Y - coordinate1.Y);
            var dPhi = Math.Log(
                Math.Tan(ToRad(coordinate2.X) / 2 + Math.PI / 4) / Math.Tan(ToRad(coordinate1.X) / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
            return ToBearing(Math.Atan2(dLon, dPhi));
        }

        static double ToRad(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        static double ToBearing(double radians)
        {
            // convert radians to degrees (as bearing: 0...360)
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
        public static double DistanceTo(this Coordinate from, Coordinate to)
        {
            if (double.IsNaN(from.X) || double.IsNaN(from.Y) || double.IsNaN(to.X) ||
                double.IsNaN(to.Y))
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
    }
}
