/*
 * This example shows how to persist data from the FlightContextFactory to a
 * database or other storage layer (πfs, anyone?).
 * 
 * Entity Framework Core in combination with Sqlite is being used to store the
 * flight metadata. In case of failure the metadata can be read from the 
 * database, and can be used to initialize the FlightContext(Factory) again.
 * 
 * This sample builds upon the RealtimeFlightActivityMonitor sample in order to
 * have a realtime data source which can be used for testing.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Boerman.AprsClient;
using Boerman.FlightAnalysis;
using Boerman.FlightAnalysis.Models;

namespace PersistentFlightContext
{
    class Program
    {
        public static Listener AprsClient;

        public static FlightContextFactory FlightContextFactory;

        static void Main(string[] args)
        {
            // On start retrieve the unfinished flights
            using (var db = new Database()) {
                var flights = db.Flights.Where(q => !q.Completed).ToList();

                FlightContextFactory = new FlightContextFactory(flights);
            }

            FlightContextFactory.OnTakeoff += async (sender, e) => {
                await StoreModelChange(e.Flight.Metadata);
            };

            FlightContextFactory.OnLanding += async (sender, e) => {
                await StoreModelChange(e.Flight.Metadata);
            };

            FlightContextFactory.OnRadarContact += async (sender, e) => {
                await StoreModelChange(e.Flight.Metadata);
            };

            FlightContextFactory.OnCompletedWithErrors += async (sender, e) => {
                await StoreModelChange(e.Flight.Metadata);
            };

            AprsClient = new Listener(new Config
            {
                Callsign = @"7CB0DBG",
                Password = "-1",
                Uri = "aprs.glidernet.org",
                UseOgnAdditives = true,
                Port = 10152
            });

            AprsClient.PacketReceived += (sender, e) => {
                if (e.AprsMessage.DataType == Boerman.AprsClient.Enums.DataType.Status) return;

                FlightContextFactory.Enqueue(new Boerman.FlightAnalysis.Models.PositionUpdate(
                    e.AprsMessage.Callsign,
                    e.AprsMessage.ReceivedDate,
                    e.AprsMessage.Latitude.AbsoluteValue,
                    e.AprsMessage.Longitude.AbsoluteValue,
                    e.AprsMessage.Altitude.FeetAboveSeaLevel,
                    e.AprsMessage.Speed.Knots,
                    e.AprsMessage.Direction.ToDegrees()));
            };

            AprsClient.Open();

            Console.WriteLine("Currently checking to see if we can receive some information!");


            Console.Read();
        }

        static async Task StoreModelChange(FlightMetadata flight) {
            using (var db = new Database())
            {
                var entry = await db.Flights.FindAsync(flight.Id);

                if (entry == null)
                {
                    await db.Flights.AddAsync(flight);
                }
                else
                {
                    db.Update(flight);
                }

                await db.SaveChangesAsync();
            }
        }
    }
}
