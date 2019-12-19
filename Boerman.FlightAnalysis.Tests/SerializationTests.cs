using Boerman.FlightAnalysis.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;
using System;

namespace Boerman.FlightAnalysis.Tests
{
    [TestClass]
    public class SerializationTests
    {
        PositionUpdate _positionUpdate = new PositionUpdate("PH-ABC", DateTime.Parse("2019-09-01T00:00:00.001"), 2, 3, 4, 5, 6);
        string _serializedPosition = "{\"Aircraft\":\"PH-ABC\",\"TimeStamp\":\"2019-09-01T00:00:00.001\",\"Latitude\":2.0,\"Longitude\":3.0,\"Altitude\":4.0,\"Speed\":5.0,\"Heading\":6.0}";
        [TestMethod]
        public void SerializePositionUpdate()
        {
            var result = JsonConvert.SerializeObject(_positionUpdate);


            Assert.AreEqual(_serializedPosition, result);
        }

        [TestMethod]
        public void DeserializePositionUpdate()
        {
            var message = "{\"Aircraft\":\"PH-ABC\",\"TimeStamp\":\"2019-09-01T00:00:00.001\",\"Latitude\":2.0,\"Longitude\":3.0,\"Altitude\":4.0,\"Speed\":5.0,\"Heading\":6.0}";

            var result = JsonConvert.DeserializeObject<PositionUpdate>(message);

            Assert.IsInstanceOfType(result, typeof(PositionUpdate));
            Assert.AreEqual(_positionUpdate.Aircraft, result.Aircraft);
            Assert.AreEqual(_positionUpdate.TimeStamp, result.TimeStamp);
            Assert.AreEqual(_positionUpdate.Latitude, result.Latitude);
            Assert.AreEqual(_positionUpdate.Longitude, result.Longitude);
            Assert.AreEqual(_positionUpdate.Altitude, result.Altitude);
            Assert.AreEqual(_positionUpdate.Speed, result.Speed);
            Assert.AreEqual(_positionUpdate.Heading, result.Heading);

            Assert.IsInstanceOfType(result.Location, typeof(Point));
        }
    }
}
