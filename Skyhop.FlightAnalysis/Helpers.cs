using System;

namespace Skyhop.FlightAnalysis
{
    public static class Helpers
    {
        public static double GetHeadingError(double initial, double final)
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
    }
}
