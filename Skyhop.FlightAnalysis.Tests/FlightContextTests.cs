using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skyhop.FlightAnalysis.Models;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public async Task Flight_D1908_20170408()
        {
            FlightContext fc = new FlightContext("6770");

            var countdownEvent = new CountdownEvent(3);

            fc.OnTakeoff += (sender, args) =>
            {
                countdownEvent.Signal();

                Assert.AreEqual(636272590876740641, ((FlightContext)sender).Flight.StartTime?.Ticks);
                Assert.AreEqual(244, ((FlightContext)sender).Flight.DepartureHeading);
            };

            fc.OnLaunchCompleted += (sender, args) =>
            {
                countdownEvent.Signal();

                Assert.AreEqual(LaunchMethods.Winch, ((FlightContext)sender).Flight.LaunchMethod);
                Assert.AreEqual(636272591994430449, ((FlightContext)sender).Flight.LaunchFinished);
            };

            fc.OnLanding += (sender, args) =>
            {
                countdownEvent.Signal();

                Assert.AreEqual(636272628474023926, ((FlightContext)sender).Flight.EndTime?.Ticks);
                Assert.AreEqual(250, ((FlightContext)sender).Flight.ArrivalHeading);
            };

            // These events should NOT be fired
            fc.OnRadarContact += (sender, e) => Assert.Fail();
            fc.OnCompletedWithErrors += (sender, e) => Assert.Fail();

            var points = Common.ReadFlightPoints("2017-04-08_D-1908.csv");
            fc.Enqueue(points);

            countdownEvent.Wait(1000);

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
        [TestMethod]
        public async Task Flight_D1908_20170408_Subset()
        {
            FlightContext fc = new FlightContext("6770");

            int callbacks = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                Assert.AreEqual(636272591685778931, ((FlightContext)sender).Flight.StartTime?.Ticks);
                Assert.AreEqual(249, ((FlightContext)sender).Flight.DepartureHeading);
                callbacks++;
            };

            fc.OnLaunchCompleted += (sender, args) =>
            {
                Assert.AreEqual(LaunchMethods.Winch, ((FlightContext)sender).Flight.LaunchMethod);
                Assert.AreEqual(636272591994430449, ((FlightContext)sender).Flight.LaunchFinished);
                callbacks++;
            };

            fc.OnLanding += (sender, args) =>
            {
                Assert.AreEqual(636272591764373758, ((FlightContext)sender).Flight.EndTime?.Ticks);
                Assert.AreEqual(244, ((FlightContext)sender).Flight.ArrivalHeading);

                callbacks++;
            };

            // These events should NOT be fired
            fc.OnRadarContact += (sender, e) => Assert.Fail();
            fc.OnCompletedWithErrors += (sender, e) => Assert.Fail();

            fc.Enqueue(Common.ReadFlightPoints("2017-04-08_D-1908.csv", true));

            Assert.AreEqual(3, callbacks);
        }

        [TestMethod]
        public void Flight_D1908_20170408_Partial()
        {
            FlightContext fc = new FlightContext("6770");

            int callbacks = 0;

            fc.OnRadarContact += (sender, args) =>
            {
                Assert.AreEqual(null, ((FlightContext)sender).Flight.StartTime);
                Assert.AreEqual(false, ((FlightContext)sender).Flight.DepartureInfoFound);
                Assert.AreEqual(0, ((FlightContext)sender).Flight.DepartureHeading);
                callbacks++;
            };

            fc.OnLanding += (sender, args) =>
            {
                Assert.AreEqual(636272628474023926, ((FlightContext)sender).Flight.EndTime?.Ticks);
                Assert.AreEqual(250, ((FlightContext)sender).Flight.ArrivalHeading);
                callbacks++;
            };

            // These events should NOT be fired 
            fc.OnTakeoff += (sender, args) => Assert.Fail();
            fc.OnCompletedWithErrors += (sender, e) => Assert.Fail();

            fc.Enqueue(Common.ReadFlightPoints("2017-04-08_D-1908.csv").Skip(500));

            Assert.AreEqual(2, callbacks);
        }

        [TestMethod]
        public void Flight_PH1387_20170421()
        {
            FlightContext fc = new FlightContext("2842");

            /*
             * In this case the tool also detects a start after the previous flight. Therefore we have to add a switch
             * statement to check in which phase we are.
             */

            int pass = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                if (pass == 0)
                {
                    Assert.AreEqual(636283687551363359, ((FlightContext)sender).Flight.StartTime?.Ticks);
                    Assert.AreEqual(355, ((FlightContext)sender).Flight.DepartureHeading);
                }
                else if (pass == 1)
                {
                    Assert.AreEqual(636283906924363860, ((FlightContext)sender).Flight.StartTime?.Ticks);
                    Assert.AreEqual(21, ((FlightContext)sender).Flight.DepartureHeading);
                }

                pass++;
            };

            fc.OnLaunchCompleted += (sender, args) =>
            {
                // If the tow would have been known, or it would have been known this aircraft is an engineless glider, it would have been obvious this self launch was a tow.
                Assert.AreEqual(LaunchMethods.Self, ((FlightContext)sender).Flight.LaunchMethod);
                Assert.AreEqual(636283689536727050, ((FlightContext)sender).Flight.LaunchFinished);

                pass++;
            };

            fc.OnLanding += (sender, args) =>
            {
                Assert.AreEqual(636283891197427348, ((FlightContext)sender).Flight.EndTime?.Ticks);
                Assert.AreEqual(338, ((FlightContext)sender).Flight.ArrivalHeading);
            };

            fc.Enqueue(Common.ReadFlightPoints("2017-04-21_PH-1387.csv"));

        }

        [TestMethod]
        public async Task Flights_PH1387_20170419_20170421()
        {
            FlightContext fc = new FlightContext("2842");

            int pass = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636281989825441178, ((FlightContext)sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(355, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636283687551363359, ((FlightContext)sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(355, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    case 2:
                        Assert.AreEqual(636283906924363860, ((FlightContext)sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(21, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    default:
                        break;
                }
            };

            fc.OnLanding += (sender, args) =>
            {
                if (pass == 0)
                {
                    Assert.AreEqual(636282163561655897, ((FlightContext)sender).Flight.EndTime?.Ticks);
                    Assert.AreEqual(339, ((FlightContext)sender).Flight.ArrivalHeading);
                }
                if (pass == 1)
                {
                    Assert.AreEqual(636283891197427348, ((FlightContext)sender).Flight.EndTime?.Ticks);
                    Assert.AreEqual(338, ((FlightContext)sender).Flight.ArrivalHeading);
                }

                pass++;
            };

            fc.Enqueue(Common.ReadFlightPoints("2017-04-19_2017-04-21_PH-1387.csv"));

        }

        [TestMethod]
        public async Task MinimalMemory_Flights_PH1387_20170419_20170421()
        {
            FlightContext fc = new FlightContext("2842", options =>
            {
                options.MinifyMemoryPressure = true;
            });

            int pass = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636281989825441178, ((FlightContext)sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(355, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636283687551363359, ((FlightContext)sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(355, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    case 2:
                        Assert.AreEqual(636283906924363860, ((FlightContext)sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(21, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    default:
                        break;
                }
            };

            fc.OnLanding += (sender, args) =>
            {
                if (pass == 0)
                {
                    Assert.AreEqual(636282163561655897, ((FlightContext)sender).Flight.EndTime?.Ticks);
                    Assert.AreEqual(339, ((FlightContext)sender).Flight.ArrivalHeading);
                }
                if (pass == 1)
                {
                    Assert.AreEqual(636283891197427348, ((FlightContext)sender).Flight.EndTime?.Ticks);
                    Assert.AreEqual(338, ((FlightContext)sender).Flight.ArrivalHeading);
                }

                pass++;
            };

            fc.Enqueue(Common.ReadFlightPoints("2017-04-19_2017-04-21_PH-1387.csv"));

        }

        [TestMethod]
        public void Flights_PH1384_20170425()
        {
            FlightContext fc = new FlightContext("5657");

            int pass = 0;
            int callbacks = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636287344501749071, ((FlightContext)sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(248, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636287368213105015, ((FlightContext)sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(246, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    case 2:
                        Assert.AreEqual(636287382263573133, ((FlightContext)sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(247, ((FlightContext)sender).Flight.DepartureHeading);
                        break;
                    case 3:
                        Assert.AreEqual(636287407314361125, ((FlightContext)sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(248, ((FlightContext)sender).Flight.DepartureHeading);
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
                        Assert.AreEqual(636287362871888759, ((FlightContext)sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(249, ((FlightContext)sender).Flight.ArrivalHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636287372913847335, ((FlightContext)sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(250, ((FlightContext)sender).Flight.ArrivalHeading);
                        break;
                    case 2:
                        Assert.AreEqual(636287393734216787, ((FlightContext)sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(250, ((FlightContext)sender).Flight.ArrivalHeading);
                        break;
                    case 3:
                        Assert.AreEqual(636287429323052452, ((FlightContext)sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(248, ((FlightContext)sender).Flight.ArrivalHeading);
                        break;
                    default:
                        break;
                }

                pass++;
                callbacks++;
            };

            fc.Enqueue(Common.ReadFlightPoints("2017-04-25_PH-1384.csv"));

            Assert.AreEqual(4, pass);
            Assert.AreEqual(8, callbacks);
        }
    }
}
