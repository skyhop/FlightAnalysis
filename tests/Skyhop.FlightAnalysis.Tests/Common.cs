using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skyhop.FlightAnalysis.Internal;
using Skyhop.FlightAnalysis.Models;

namespace Skyhop.FlightAnalysis.Tests
{
    internal static class Common
    {
        internal static string ReadFile(string filename)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            return File.ReadAllText(Path.Combine(path, "Dependencies", filename));
        }

        internal static IEnumerable<PositionUpdate> ReadFlightPoints(string filename, bool useSubset = false)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var lines = File.ReadAllLines(Path.Combine(path,
                "Dependencies", filename)).Skip(1);

            foreach (var line in lines)
            {
                var fields = line.Split(',');
                var location = fields[9].HexStringToByteArray();

                var x = BitConverter.ToDouble(location, 6);
                var y = BitConverter.ToDouble(location, 14);

                if (useSubset)
                {
                    yield return new PositionUpdate(fields[1], new DateTime(long.Parse(fields[10])), x, y);
                    continue;
                }

                yield return new PositionUpdate(
                    fields[1],
                    new DateTime(long.Parse(fields[10])),
                    x,
                    y,
                    int.Parse(fields[3]),
                    int.Parse(fields[5]),
                    int.Parse(fields[6]));
            }
        }

        internal static void CompareDeparture(this FlightContext context, short heading, long ticks)
        {
            Assert.AreEqual(heading, context.Flight.DepartureHeading);
            Assert.AreEqual(ticks, context.Flight.DepartureTime);
        }

        internal static void CompareArrival(this FlightContext context, short heading, long ticks)
        {
            Assert.AreEqual(heading, context.Flight.ArrivalHeading);
            Assert.AreEqual(ticks, context.Flight.ArrivalTime);
        }
    }
}
