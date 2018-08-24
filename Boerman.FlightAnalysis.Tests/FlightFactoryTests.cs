using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Boerman.FlightAnalysis.Tests
{
    [TestClass]
    public class FlightFactoryTests
    {
        [TestMethod]
        public void TestFlightFactory()
        {
            var ff = new FlightContextFactory();
            
            // ToDo: Verify that all the data is being processed correctly.

            /*
             * The definition of correct is that there should be different context instances for different aircraft.
             * Next the start/end times, heading and locations should be correct and the events should be firing.
             * The context factory should be capable of processing a lot of mixed information from different aircraft.
             */
            
            ff.OnTakeoff += (sender, args) =>
            {
                
            };

            ff.OnLanding += (sender, args) =>
            {

            };

            ff.Enqueue(Common.ReadFlightPoints("2017-04-08_D-1908.csv"));
            ff.Enqueue(Common.ReadFlightPoints("2017-04-21_PH-1387.csv"));
            ff.Enqueue(Common.ReadFlightPoints("2017-04-19_2017-04-21_PH-1387.csv"));
            ff.Enqueue(Common.ReadFlightPoints("2017-04-25_PH-1384.csv"));
        }
    }
}
