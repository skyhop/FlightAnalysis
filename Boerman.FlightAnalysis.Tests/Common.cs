using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Boerman.FlightAnalysis.Models;
using Boerman.Core.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Boerman.FlightAnalysis.Tests
{
    internal static class Common
    {
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
                    yield return new PositionUpdate(fields[1], new DateTime(Int64.Parse(fields[10])),  x, y);

                yield return new PositionUpdate(
                    fields[1],
                    new DateTime(Int64.Parse(fields[10])),
                    x,
                    y,
                    Int32.Parse(fields[3]),
                    Int32.Parse(fields[5]),
                    Int32.Parse(fields[6]));
            }
        }

        internal static void CompareDeparture(this FlightContext context, short heading, long ticks)
        {
            Assert.AreEqual(heading, context.Flight.DepartureHeading);
            Assert.AreEqual(ticks, context.Flight.StartTime);
        }

        internal static void CompareArrival(this FlightContext context, short heading, long ticks)
        {
            Assert.AreEqual(heading, context.Flight.ArrivalHeading);
            Assert.AreEqual(ticks, context.Flight.EndTime);
        }
    }
}
