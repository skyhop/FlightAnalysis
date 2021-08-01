using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTopologySuite.Geometries;
using Skyhop.FlightAnalysis.Models;
using Skyhop.Igc;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Skyhop.FlightAnalysis.Tests
{
    /*
     * WARNING:
     *
     * The logic which fires event handlers from the FlightContext and FlightContextFactory instances is
     * wrapped with try/catch blocks in order to prevent user implemented bugs from making it look as if the
     * FlightAnalysis tool itself has crashed.
     *
     * It'd be nice if we could implement something which would make it obvious to the implementing party
     * that something in their code goes wrong while gracefully handling it.
     *
     * The side effect of this is that Assert functions will not immediately fail the test case when used
     * inside an event handler.
     */

    [TestClass]
    public class FlightContextTests
    {          
        [TestMethod]
        public void Flight_D1908_20170408()
        {
            var fc = new FlightContext("6770");  

            var countdownEvent = new CountdownEvent(3);

            fc.OnTakeoff += (sender, args) =>
            {
                countdownEvent.Signal();

                Assert.AreEqual(636272591685778931, ((FlightContext)sender).Flight.DepartureTime?.Ticks);
                Assert.AreEqual(new Point(4.35716666666667, 51.4516666666667, 30), ((FlightContext)sender).Flight.DepartureLocation);
            };

            fc.OnLaunchCompleted += (sender, args) =>
            {
                countdownEvent.Signal();

                Assert.AreEqual(LaunchMethods.Winch, ((FlightContext)sender).Flight.LaunchMethod);
                Assert.AreEqual(636272591835295201, ((FlightContext)sender).Flight.LaunchFinished?.Ticks);
                Assert.AreEqual(244, ((FlightContext)sender).Flight.DepartureHeading);
            };

            fc.OnLanding += (sender, args) =>
            {
                countdownEvent.Signal();

                Assert.AreEqual(636272628474023926, ((FlightContext)sender).Flight.ArrivalTime?.Ticks);
                Assert.AreEqual(250, ((FlightContext)sender).Flight.ArrivalHeading);
                Assert.AreEqual(new Point(4.35583333333333, 51.451, 39), ((FlightContext)sender).Flight.ArrivalLocation);
            };

            // These events should NOT be fired
            fc.OnRadarContact += (sender, e) => Assert.Fail();
            fc.OnCompletedWithErrors += (sender, e) => Assert.Fail();

            var points = Common.ReadFlightPoints("2017-04-08_D-1908.csv");
            var flights = fc.Process(points);

            Assert.AreEqual(flights.Count(), 1);
            Assert.IsTrue(countdownEvent.CurrentCount == 0);
        }

        /*
         * While this test is pretty well similar to the previous test, some of the data used (speed /
         * heading) is extracted from the lat/long  points. Therefore, results may not be 100% the same.
         *
         * For now the departure and arrival times seems to be the same, while there is a small
         * discrepancy in the observed heading for takeoff and landing (+- 10 degrees).
         *
         * This might be due to either of those reasons:
         * - FLARM units transmit their heading based on an internall compass (?)
         * - I'm using a rhumb line for heading calculations. However I don't think this makes up for a
         *   10 degree discrepancy...
         * 
         * 
         * FAILING TEST:
         * 
         * The main reason to blame this failing test is to the way FLARM collects and sentds it's data.
         * It turns out the GPS fix is not updated nearly enough. Because of this the position is the same, 
         * and therefore when calculating the difference in position we'll get '0', because of which the algorithm
         * thinks the aircraft has stopped.
         * 
         * There are several possible solutions:
         * 
         * 1. Ignore points which are quicker than x time period in succession.
         * 2. Determine the maximum G load for a specific operation, and ignore anything out of bounds.
         * 
         * So long as this hasn't been solved we're disabling this test method.
         * 
         */
        //[TestMethod]
        public void Flight_D1908_20170408_Subset()
        {
            var fc = new FlightContext("6770");

            int callbacks = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                Assert.AreEqual(636272591485655057, ( (FlightContext)sender ).Flight.DepartureTime?.Ticks);
                Assert.AreEqual(195, ( (FlightContext)sender ).Flight.DepartureHeading);
                callbacks++;
            };

            fc.OnLaunchCompleted += (sender, args) =>
            {
                Assert.AreEqual(LaunchMethods.Winch, ( (FlightContext)sender ).Flight.LaunchMethod);
                Assert.AreEqual(636272591994430449, ( (FlightContext)sender ).Flight.LaunchFinished?.Ticks);
                callbacks++;
            };

            fc.OnLanding += (sender, args) =>
            {
                Assert.AreEqual(636272591764373758, ( (FlightContext)sender ).Flight.ArrivalTime?.Ticks);
                Assert.AreEqual(244, ( (FlightContext)sender ).Flight.ArrivalHeading);

                callbacks++;
            };

            // These events should NOT be fired
            fc.OnRadarContact += (sender, e) => Assert.Fail();
            fc.OnCompletedWithErrors += (sender, e) => Assert.Fail();

            var flights = fc.Process(Common.ReadFlightPoints("2017-04-08_D-1908.csv", true));

            Assert.AreEqual(1, flights.Count());
            Assert.AreEqual(3, callbacks);
        }

        [TestMethod]
        public void Flight_D1908_20170408_Partial()
        {
            var fc = new FlightContext("6770");

            int callbacks = 0;

            fc.OnRadarContact += (sender, args) =>
            {
                Assert.AreEqual(null, ((FlightContext)sender).Flight.DepartureTime);
                Assert.AreEqual(false, ((FlightContext)sender).Flight.DepartureInfoFound);
                Assert.AreEqual(0, ((FlightContext)sender).Flight.DepartureHeading);
                callbacks++;
            };

            fc.OnLanding += (sender, args) =>
            {
                Assert.AreEqual(636272628474023926, ((FlightContext)sender).Flight.ArrivalTime?.Ticks);
                Assert.AreEqual(250, ((FlightContext)sender).Flight.ArrivalHeading);
                callbacks++;
            };

            // These events should NOT be fired 
            fc.OnTakeoff += (sender, args) => Assert.Fail();
            fc.OnCompletedWithErrors += (sender, e) => Assert.Fail();

            var flights = fc.Process(Common.ReadFlightPoints("2017-04-08_D-1908.csv").Skip(500));

            Assert.AreEqual(1, flights.Count());
            Assert.AreEqual(2, callbacks);
        }

        [TestMethod]
        public void Flight_PH1387_20170421()
        {
            var fc = new FlightContext("2842");

            /*
             * In this case the tool also detects a start after the previous flight. Therefore we have to add a switch
             * statement to check in which phase we are.
             */

            int pass = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                if (pass == 0)
                {
                    Assert.AreEqual(636283687551363359, ((FlightContext)sender).Flight.DepartureTime?.Ticks);
                    Assert.AreEqual(354, ((FlightContext)sender).Flight.DepartureHeading);
                }
                else if (pass == 1)
                {
                    Assert.AreEqual(636283906924363860, ((FlightContext)sender).Flight.DepartureTime?.Ticks);
                    Assert.AreEqual(21, ((FlightContext)sender).Flight.DepartureHeading);
                }

                pass++;
            };

            fc.OnLaunchCompleted += (sender, args) =>
            {
                // If the tow would have been known, or it would have been known this aircraft is an engineless glider, it would have been obvious this self launch was a tow.
                Assert.AreEqual(LaunchMethods.Winch, ((FlightContext)sender).Flight.LaunchMethod);
                Assert.AreEqual(636283688058667865, ((FlightContext)sender).Flight.LaunchFinished?.Ticks);

                pass++;
            };

            fc.OnLanding += (sender, args) =>
            {
                Assert.AreEqual(636283891197427348, ((FlightContext)sender).Flight.ArrivalTime?.Ticks);
                Assert.AreEqual(338, ((FlightContext)sender).Flight.ArrivalHeading);
            };

            var flights = fc.Process(Common.ReadFlightPoints("2017-04-21_PH-1387.csv"));

            Assert.AreEqual(1, flights.Count());
        }

        [TestMethod]
        public void Flights_PH1387_20170419_20170421()
        {
            var fc = new FlightContext("2842");

            int pass = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636281989825441178, ( (FlightContext)sender ).Flight.DepartureTime?.Ticks);
                        Assert.AreEqual(356, ( (FlightContext)sender ).Flight.DepartureHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636283687551363359, ( (FlightContext)sender ).Flight.DepartureTime?.Ticks);
                        Assert.AreEqual(354, ( (FlightContext)sender ).Flight.DepartureHeading);
                        break;
                    default:
                        Assert.Fail();
                        break;
                }
            };

            //fc.OnLaunchCompleted += (sender, args) =>
            //{
            //    switch (pass)
            //    {
            //        case 0:
            //            Assert.AreEqual(LaunchMethods.Self, ((Experimental.FlightContext)sender).Flight.LaunchMethod);
            //            Assert.AreEqual(636281993021809484, ((Experimental.FlightContext)sender).Flight.LaunchFinished?.Ticks);
            //            break;
            //        case 1:
            //            Assert.AreEqual(LaunchMethods.Self, ((Experimental.FlightContext)sender).Flight.LaunchMethod);
            //            Assert.AreEqual(636283689536727050, ((Experimental.FlightContext)sender).Flight.LaunchFinished?.Ticks);
            //            break;
            //        default:
            //            Assert.Fail();
            //            break;
            //    }
            //};

            fc.OnLanding += (sender, args) =>
            {
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636282163561655897, ( (FlightContext)sender ).Flight.ArrivalTime?.Ticks);
                        Assert.AreEqual(339, ( (FlightContext)sender ).Flight.ArrivalHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636283891197427348, ( (FlightContext)sender ).Flight.ArrivalTime?.Ticks);
                        Assert.AreEqual(338, ( (FlightContext)sender ).Flight.ArrivalHeading);
                        break;
                    default:
                        Assert.Fail();
                        break;
                }

                pass++;
            };

            var flights = fc.Process(Common.ReadFlightPoints("2017-04-19_2017-04-21_PH-1387.csv"));
            Assert.AreEqual(2, flights.Count());
        }

        [TestMethod]
        public void MinimalMemory_Flights_PH1387_20170419_20170421()
        {
            var fc = new FlightContext("2842", options =>
            {
            });

            int pass = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636281989825441178, ( (FlightContext)sender ).Flight.DepartureTime?.Ticks);
                        Assert.AreEqual(356, ( (FlightContext)sender ).Flight.DepartureHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636283687551363359, ( (FlightContext)sender ).Flight.DepartureTime?.Ticks);
                        Assert.AreEqual(354, ( (FlightContext)sender ).Flight.DepartureHeading);
                        break;
                    case 2:
                        Assert.AreEqual(636283906924363860, ( (FlightContext)sender ).Flight.DepartureTime?.Ticks);
                        Assert.AreEqual(21, ( (FlightContext)sender ).Flight.DepartureHeading);
                        break;
                    default:
                        break;
                }
            };

            fc.OnLanding += (sender, args) =>
            {
                if (pass == 0)
                {
                    Assert.AreEqual(636282163561655897, ( (FlightContext)sender ).Flight.ArrivalTime?.Ticks);
                    Assert.AreEqual(339, ( (FlightContext)sender ).Flight.ArrivalHeading);
                }
                if (pass == 1)
                {
                    Assert.AreEqual(636283891197427348, ( (FlightContext)sender ).Flight.ArrivalTime?.Ticks);
                    Assert.AreEqual(338, ( (FlightContext)sender ).Flight.ArrivalHeading);
                }

                pass++;
            };

            var flights = fc.Process(Common.ReadFlightPoints("2017-04-19_2017-04-21_PH-1387.csv"));
            Assert.AreEqual(2, flights.Count());
        }

        [TestMethod]
        public void Flights_PH1384_20170425()
        {
            var fc = new FlightContext("5657");

            int pass = 0;
            int callbacks = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636287344501749071, ((FlightContext)sender).Flight.DepartureTime?.Ticks);
                        Assert.AreEqual(249, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636287368213105015, ((FlightContext)sender).Flight.DepartureTime?.Ticks);
                        Assert.AreEqual(246, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    case 2:
                        Assert.AreEqual(636287382263573133, ((FlightContext)sender).Flight.DepartureTime?.Ticks);
                        Assert.AreEqual(246, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    case 3:
                        Assert.AreEqual(636287407314361125, ((FlightContext)sender).Flight.DepartureTime?.Ticks);
                        Assert.AreEqual(247, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    default:
                        break;
                }

                callbacks++;
            };

            fc.OnLanding += (sender, args) =>
            {
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636287362871888759, ((FlightContext)sender).Flight.ArrivalTime?.Ticks);
                        Assert.AreEqual(249, ((FlightContext)sender).Flight.ArrivalHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636287372913847335, ((FlightContext)sender).Flight.ArrivalTime?.Ticks);
                        Assert.AreEqual(250, ((FlightContext)sender).Flight.ArrivalHeading);
                        break;
                    case 2:
                        Assert.AreEqual(636287393734216787, ((FlightContext)sender).Flight.ArrivalTime?.Ticks);
                        Assert.AreEqual(250, ((FlightContext)sender).Flight.ArrivalHeading);
                        break;
                    case 3:
                        Assert.AreEqual(636287429323052452, ((FlightContext)sender).Flight.ArrivalTime?.Ticks);
                        Assert.AreEqual(248, ((FlightContext)sender).Flight.ArrivalHeading);
                        break;
                    default:
                        break;
                }

                pass++;
                callbacks++;
            };

            var flights = fc.Process(Common.ReadFlightPoints("2017-04-25_PH-1384.csv"));

            Assert.AreEqual(4, flights.Count());
            Assert.AreEqual(4, pass);
            Assert.AreEqual(8, callbacks);
        }

        [TestMethod]
        public void CanProcessIgcFile()
        {
            var fileContents = Common.ReadFile("20-09-19 17_31 PH-975.igc");

            var igcFile = Parser.Parse(fileContents);

            var context = new FlightContext(igcFile.Registration);

            var positionUpdates = igcFile.Fixes.Select(q =>
                new PositionUpdate(
                    igcFile.Registration,
                    q.Timestamp,
                    q.Latitude,
                    q.Longitude,
                    q.PressureAltitude ?? q.GpsAltitude ?? double.NaN,
                    double.NaN,
                    double.NaN
                )
            ).ToList();

            var processedFlights = context.Process(positionUpdates);

            Assert.IsTrue(context.Flight != null);
        }

        [TestMethod]
        public void TestIgcFile_20180427()
        {
            // This flight is a special case, as it's a paraglide flight.
            // In order to be able to recognize this flight correctly, we'd need to allow a lower takeoff speed.
            var fileContents = Common.ReadFile("20180427.igc");

            var igcFile = Parser.Parse(fileContents);

            var context = new FlightContext(igcFile.Registration);

            var positionUpdates = igcFile.Fixes.Select(q =>
                new PositionUpdate(
                    igcFile.Registration,
                    q.Timestamp,
                    q.Latitude,
                    q.Longitude,
                    q.PressureAltitude ?? q.GpsAltitude ?? double.NaN,
                    double.NaN,
                    double.NaN
                )
            ).ToList();

            
            context.Process(positionUpdates);

            // ToDo: Add assertions

            
        }

        [TestMethod]
        public void TestIgcFile_654G6NG1()
        {
            var fileContents = Common.ReadFile("654G6NG1.IGC");

            var igcFile = Parser.Parse(fileContents);

            var context = new FlightContext(igcFile.Registration);

            var positionUpdates = igcFile.Fixes.Select(q =>
                new PositionUpdate(
                    igcFile.Registration,
                    q.Timestamp,
                    q.Latitude,
                    q.Longitude,
                    q.PressureAltitude ?? q.GpsAltitude ?? double.NaN,
                    double.NaN,
                    double.NaN
                )
            ).ToList();

            var flight = context
                .Process(positionUpdates)
                .SingleOrDefault();

            Assert.IsTrue(flight.Aircraft == "D-KCSS");
            Assert.IsTrue(flight.ArrivalHeading == 249);
            Assert.IsTrue(flight.ArrivalInfoFound == true);
            Assert.IsTrue(flight.ArrivalTime == DateTime.Parse("04/05/2016 20:59:58"));
            Assert.IsTrue(flight.Completed == true);
            Assert.IsTrue(flight.DepartureHeading == 13);
            Assert.IsTrue(flight.DepartureTime == DateTime.Parse("04/05/2016 10:12:26"));
            Assert.IsTrue(flight.LaunchFinished == DateTime.Parse("04/05/2016 10:18:18"));
            Assert.IsTrue(flight.LaunchMethod == LaunchMethods.Winch); // Dubious choice, but okay
            Assert.IsTrue(flight.PositionUpdates.Count == 9711);
        }

        [TestMethod]
        public void TestIgcFile_20161108xcsaaa02()
        {
            var fileContents = Common.ReadFile("2016-11-08-xcs-aaa-02.igc");

            var igcFile = Parser.Parse(fileContents);

            var context = new FlightContext(igcFile.Registration);

            var positionUpdates = igcFile.Fixes.Select(q =>
                new PositionUpdate(
                    igcFile.Registration,
                    q.Timestamp,
                    q.Latitude,
                    q.Longitude,
                    q.PressureAltitude ?? q.GpsAltitude ?? double.NaN,
                    double.NaN,
                    double.NaN
                )
            ).ToList();

            var flight= context
                .Process(positionUpdates)
                .SingleOrDefault();

            Assert.IsTrue(flight.Aircraft == "DUO");
            Assert.IsTrue(flight.ArrivalHeading == 55);
            Assert.IsTrue(flight.ArrivalInfoFound == true);
            Assert.IsTrue(flight.ArrivalTime == DateTime.Parse("09/11/2016 05:42:51"));
            Assert.IsTrue(flight.Completed == true);
            Assert.IsTrue(flight.DepartureHeading == 17);
            Assert.IsTrue(flight.DepartureTime == DateTime.Parse("08/11/2016 23:47:47"));
            Assert.IsTrue(flight.LaunchFinished == DateTime.Parse("08/11/2016 23:52:37"));
            Assert.IsTrue(flight.LaunchMethod == LaunchMethods.Self); // Dubious choice, but okay
            Assert.IsTrue(flight.PositionUpdates.Count == 6695);
        }
    }
}
