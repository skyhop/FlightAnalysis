using Boerman.FlightAnalysis.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Boerman.FlightAnalysis.Tests
{
    [TestClass]
    public class FlightFactoryTests
    {
        [TestMethod]
        public void TestFlightFactory()
        {
            try
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
            } catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod]
        public void AttachEmptyMetadataObject()
        {
            try
            {
                var ff = new FlightContextFactory();

                var flightMetadata = new FlightMetadata
                {
                    Aircraft = "FLRDD056A",
                    Id = Guid.NewGuid()
                };

                ff.Attach(flightMetadata);
            }
            catch (Exception ex)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void AttachEmptyMetadataObjectToFactory()
        {
            try
            {
                var ff = new FlightContextFactory();

                var metadata = new FlightMetadata
                {
                    Aircraft = "FLRDD056A"
                };

                ff.Attach(metadata);
            }
            catch (Exception ex)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void AttachEmptyFlightContextObjectToFactory()
        {
            try
            {
                var ff = new FlightContextFactory();

                var context = new FlightContext(new FlightMetadata
                {
                    Aircraft = "FLRDD056A"
                });

                ff.Attach(context);
            } catch (Exception ex)
            {
                Assert.Fail();
            }
        }
    }
}
