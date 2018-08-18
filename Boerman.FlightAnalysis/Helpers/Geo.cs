using System;
using System.Collections.Generic;
using System.Text;

namespace Boerman.FlightAnalysis.Helpers
{
    public static class Geo
    {
        // See https://stackoverflow.com/a/2042883/1720761 for more information about these methods.

        public static double DegreeBearing(
            GeoCoordinate coordinate1,
            GeoCoordinate coordinate2)
        {
            var dLon = ToRad(coordinate2.Longitude - coordinate1.Longitude);
            var dPhi = Math.Log(
                Math.Tan(ToRad(coordinate2.Latitude) / 2 + Math.PI / 4) / Math.Tan(ToRad(coordinate1.Latitude) / 2 + Math.PI / 4));
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
    }
}
