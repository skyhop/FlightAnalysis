using System;
using System.Globalization;
using System.Linq;

namespace Skyhop.FlightAnalysis.Internal
{
    public static class StringExtensions
    {
        public static byte[] HexStringToByteArray(this string hex)
        {
            if (hex.StartsWith("0x", true, CultureInfo.InvariantCulture))
                hex = hex.Remove(0, 2);

            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
