using System.Threading.Tasks;
using Boerman.Aeronautics.FlightAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Boerman.Aeronautics.FlightAnalyis.Tests
{
    [TestClass]
    public class FlightContextTests
    {
        [TestMethod]
        public void Flight_D1908_20170408()
        {
            FlightContext fc = new FlightContext("6770");

            fc.OnTakeoff += (sender, args) =>
            {
                Assert.AreEqual(636272591685778931, ((FlightContext) sender).Flight.StartTime?.Ticks);
                Assert.AreEqual(244, ((FlightContext) sender).Flight.DepartureHeading);
            };

            fc.OnLanding += (sender, args) =>
            {
                Assert.AreEqual(636272628474023926, ((FlightContext) sender).Flight.EndTime?.Ticks);
                Assert.AreEqual(250, ((FlightContext) sender).Flight.ArrivalHeading);
            };

            fc.Enqueue(Common.ReadFlightPoints("2017-04-08_D-1908.csv"));

            fc.WaitForIdleProcess.WaitOne();
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
            };

            fc.Enqueue(Common.ReadFlightPoints("2017-04-21_PH-1387.csv"));

            fc.WaitForIdleProcess.WaitOne();
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

            fc.WaitForIdleProcess.WaitOne();
        }

        [TestMethod]
        public void Flights_PH1384_20170425()
        {
            FlightContext fc = new FlightContext("5657");

            int pass = 0;

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
            };

            fc.Enqueue(Common.ReadFlightPoints("2017-04-25_PH-1384.csv"));

            fc.WaitForIdleProcess.WaitOne();
        }
    }
}
