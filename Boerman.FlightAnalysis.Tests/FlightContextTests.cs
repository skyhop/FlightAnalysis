using System.Linq;
using System.Threading.Tasks;
using Boerman.FlightAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Boerman.FlightAnalysis.Tests
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
            FlightContext fc = new FlightContext("6770");

            int callbacks = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                Assert.AreEqual(636272591685778931, ((FlightContext) sender).Flight.StartTime?.Ticks);
                Assert.AreEqual(244, ((FlightContext) sender).Flight.DepartureHeading);
                callbacks++;
            };

            fc.OnLanding += (sender, args) =>
            {
                Assert.AreEqual(636272628474023926, ((FlightContext) sender).Flight.EndTime?.Ticks);
                Assert.AreEqual(250, ((FlightContext) sender).Flight.ArrivalHeading);
                
                callbacks++;
            };

            // These events should NOT be fired
            fc.OnRadarContact += (sender, e) => Assert.Fail();
            fc.OnCompletedWithErrors += (sender, e) => Assert.Fail();

            fc.Enqueue(Common.ReadFlightPoints("2017-04-08_D-1908.csv"));

            fc.WaitForIdleProcess();

            Assert.AreEqual(2, callbacks);
        }
        
        [TestMethod]
        public void Flight_D1908_20170408_Subset()
        {
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
             */

            FlightContext fc = new FlightContext("6770");

            int callbacks = 0;

            fc.OnTakeoff += (sender, args) =>
            {
                Assert.AreEqual(636272591685778931, ((FlightContext)sender).Flight.StartTime?.Ticks);
                Assert.AreEqual(249, ((FlightContext)sender).Flight.DepartureHeading);
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

            fc.WaitForIdleProcess();

            Assert.AreEqual(2, callbacks);
        }

        [TestMethod]
        public void Flight_D1908_20170408_Partial() {
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

            fc.WaitForIdleProcess();

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
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636283687551363359, ((FlightContext) sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(355, ((FlightContext) sender).Flight.DepartureHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636283906924363860, ((FlightContext) sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(21, ((FlightContext) sender).Flight.DepartureHeading);
                        break;
                }
                pass++;
            };

            fc.OnLanding += (sender, args) =>
            {
                Assert.AreEqual(636283891197427348, ((FlightContext) sender).Flight.EndTime?.Ticks);
                Assert.AreEqual(338, ((FlightContext) sender).Flight.ArrivalHeading);


                var kml = ((FlightContext)sender).Flight.ViewModel.PositionUpdates.AsKmlXml();

            };

            fc.Enqueue(Common.ReadFlightPoints("2017-04-21_PH-1387.csv"));

            fc.WaitForIdleProcess();
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
                        Assert.AreEqual(636281989825441178, ((FlightContext) sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(355, ((FlightContext) sender).Flight.DepartureHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636283687551363359, ((FlightContext) sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(355, ((FlightContext) sender).Flight.DepartureHeading);
                        break;
                    case 2:
                        Assert.AreEqual(636283906924363860, ((FlightContext) sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(21, ((FlightContext) sender).Flight.DepartureHeading);
                        break;
                }
            };

            fc.OnLanding += (sender, args) =>
            {
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636282163561655897, ((FlightContext) sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(339, ((FlightContext) sender).Flight.ArrivalHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636283891197427348, ((FlightContext) sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(338, ((FlightContext) sender).Flight.ArrivalHeading);
                        break;
                }

                pass++;
            };

            fc.Enqueue(Common.ReadFlightPoints("2017-04-19_2017-04-21_PH-1387.csv"));

            fc.WaitForIdleProcess();
        }

        [TestMethod]
        public async Task MinimalMemory_Flights_PH1387_20170419_20170421()
        {
            FlightContext fc = new FlightContext("2842", true);

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
                }
            };

            fc.OnLanding += (sender, args) =>
            {
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636282163561655897, ((FlightContext)sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(339, ((FlightContext)sender).Flight.ArrivalHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636283891197427348, ((FlightContext)sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(338, ((FlightContext)sender).Flight.ArrivalHeading);
                        break;
                }

                pass++;
            };

            fc.Enqueue(Common.ReadFlightPoints("2017-04-19_2017-04-21_PH-1387.csv"));

            fc.WaitForIdleProcess();
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
                        Assert.AreEqual(636287344501749071, ((FlightContext) sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(248, ((FlightContext) sender).Flight.DepartureHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636287368213105015, ((FlightContext) sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(246, ((FlightContext) sender).Flight.DepartureHeading);
                        break;
                    case 2:
                        Assert.AreEqual(636287382263573133, ((FlightContext) sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(247, ((FlightContext) sender).Flight.DepartureHeading);
                        break;
                    case 3:
                        Assert.AreEqual(636287407314361125, ((FlightContext) sender).Flight.StartTime?.Ticks);
                        Assert.AreEqual(248, ((FlightContext) sender).Flight.DepartureHeading);
                        break;
                }

                callbacks++;
            };

            fc.OnLanding += (sender, args) =>
            {
                switch (pass)
                {
                    case 0:
                        Assert.AreEqual(636287362871888759, ((FlightContext) sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(249, ((FlightContext) sender).Flight.ArrivalHeading);
                        break;
                    case 1:
                        Assert.AreEqual(636287372913847335, ((FlightContext) sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(250, ((FlightContext) sender).Flight.ArrivalHeading);
                        break;
                    case 2:
                        Assert.AreEqual(636287393734216787, ((FlightContext) sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(250, ((FlightContext) sender).Flight.ArrivalHeading);
                        break;
                    case 3:
                        Assert.AreEqual(636287429323052452, ((FlightContext) sender).Flight.EndTime?.Ticks);
                        Assert.AreEqual(248, ((FlightContext) sender).Flight.ArrivalHeading);
                        break;
                }

                pass++;
                callbacks++;
            };

            fc.Enqueue(Common.ReadFlightPoints("2017-04-25_PH-1384.csv"));

            fc.WaitForIdleProcess();

            Assert.AreEqual(4, pass);
            Assert.AreEqual(8, callbacks);
        }
    }
}
