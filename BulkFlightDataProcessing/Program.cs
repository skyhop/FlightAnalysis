using CsvHelper;
using NetTopologySuite.Geometries;
using Skyhop.FlightAnalysis;
using Skyhop.FlightAnalysis.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BulkFlightDataProcessing
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var ff = new FlightContextFactory(options =>
            {
                options.NearbyRunwayAccessor = (Point point, double distance) =>
                {
                    return new[]
                    {
                        new Runway(
                            new Point(4.942108, 51.572418, 49),
                            new Point(4.933319, 51.555660, 49)
                        ),
                        new Runway(
                            new Point(4.950768, 51.565256, 49),
                            new Point(4.911974, 51.569548, 49))
                    };
                };
            });

            using (var reader = new StreamReader(@"C:\Users\Corstian\Projects\Whaally\Skyhop\EHGR-Sept.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            using (var writer = new StreamWriter("./experimental-logs-sept-5.csv"))
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var lines = csv.GetRecords<CsvData>();

                var departureCounter = 0;
                var arrivalCounter = 0;

                ff.OnTakeoff += (sender, args) =>
                {
                    departureCounter++;
                };

                ff.OnLanding += (sender, args) =>
                {
                    arrivalCounter++;

                    var data = new
                    {
                        args.Flight.Aircraft,
                        args.Flight.DepartureHeading,
                        DepartureX = args.Flight.DepartureLocation?.X,
                        DepartureY = args.Flight.DepartureLocation?.Y,
                        args.Flight.StartTime,
                        args.Flight.DepartureInfoFound,
                        args.Flight.LaunchMethod,
                        args.Flight.ArrivalHeading,
                        ArrivalX = args.Flight.ArrivalLocation?.X,
                        ArrivalY = args.Flight.ArrivalLocation?.Y,
                        args.Flight.EndTime,
                        args.Flight.ArrivalInfoFound,
                    };

                    csvWriter.WriteRecord(data);
                    csvWriter.NextRecord();
                    csvWriter.Flush();
                };

                var timestamp = DateTime.Parse("2020-09-12T09:00:01");

                ff.Process(lines
                    //.Where(q => q.Timestamp > timestamp)
                    .Select(q => new PositionUpdate(q.Aircraft, q.Timestamp, q.Longitude, q.Latitude, q.Altitude, q.Speed, q.Heading))
                    .ToList());

                Console.WriteLine(departureCounter);
                Console.WriteLine(arrivalCounter);
            }
        }
    }
}
