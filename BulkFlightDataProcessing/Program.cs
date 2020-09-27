using CsvHelper;
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

            });

            using (var reader = new StreamReader(@"C:\Users\Corstian\Projects\Whaally\Skyhop\EHGR-August.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            using (var writer = new StreamWriter("./logs.csv"))
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var lines = csv.GetRecords<CsvData>();

                var positionUpdates = lines
                    .Select(q => new PositionUpdate(q.Aircraft, q.Timestamp, q.Latitude, q.Longitude, q.Altitude, q.Speed, q.Heading))
                    .ToList();

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
                        args.Flight.LaunchMethod,
                        args.Flight.ArrivalHeading,
                        ArrivalX = args.Flight.ArrivalLocation?.X,
                        ArrivalY = args.Flight.ArrivalLocation?.Y,
                        args.Flight.EndTime
                    };

                    csvWriter.WriteRecord(data);
                    csvWriter.NextRecord();
                    csvWriter.Flush();
                };

                ff.Enqueue(positionUpdates);

                Console.WriteLine(departureCounter);
                Console.WriteLine(arrivalCounter);
            }
        }
    }
}
