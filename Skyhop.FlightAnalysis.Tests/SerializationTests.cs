using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;
using System;
using Skyhop.FlightAnalysis.Models;

namespace Skyhop.FlightAnalysis.Tests
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
            var result = JsonConvert.DeserializeObject<PositionUpdate>(_serializedPosition);

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

        FlightMetadata flightMetadata = new FlightMetadata()
        {
            Aircraft = "PH-ABC",
            ArrivalHeading = 205,
            ArrivalInfoFound = true,
            ArrivalLatitude = 1,
            ArrivalLongitude = 2,
            ArrivalTime = DateTime.Parse("2019-09-01T00:00:00.001"),
            DepartureHeading = 205,
            DepartureInfoFound = true,
            DepartureLatitude = 2,
            DepartureLongitude = 4,
            DepartureTime = DateTime.Parse("2019-09-01T00:00:00.002"),
            Id = Guid.Parse("{D45455FB-C2CD-46AA-8F3D-8A3F8E1A02B8}"),
            LastSeen = DateTime.Parse("2019-09-01T00:00:00.100")
        };

        string serializedMetadata = "{\"Id\":\"d45455fb-c2cd-46aa-8f3d-8a3f8e1a02b8\",\"Aircraft\":\"PH-ABC\",\"LastSeen\":\"2019-09-01T00:00:00.1\",\"DepartureTime\":\"2019-09-01T00:00:00.002\",\"DepartureHeading\":205,\"DepartureLatitude\":2.0,\"DepartureLongitude\":4.0,\"DepartureInfoFound\":true,\"ArrivalTime\":\"2019-09-01T00:00:00.001\",\"ArrivalHeading\":205,\"ArrivalLatitude\":1.0,\"ArrivalLongitude\":2.0,\"ArrivalInfoFound\":true,\"Completed\":true}";

        [TestMethod]
        public void SerializeFlightMetdata()
        {
            var result = JsonConvert.SerializeObject(flightMetadata);

            Assert.AreEqual(serializedMetadata, result);
        }

        public void DeserializeFlightMetadata()
        {
            var result = JsonConvert.DeserializeObject<FlightMetadata>(serializedMetadata);

            Assert.IsInstanceOfType(result, typeof(FlightMetadata));
            Assert.IsInstanceOfType(result.ArrivalLocation, typeof(Point));
            Assert.IsInstanceOfType(result.DepartureLocation, typeof(Point));
        }
    }
}
