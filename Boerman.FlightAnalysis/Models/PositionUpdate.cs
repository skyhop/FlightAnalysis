using System;

namespace Boerman.FlightAnalysis.Models
{
    public class PositionUpdate
    {
        public PositionUpdate(string aircraft, DateTime timeStamp, double latitude, double longitude, double altitude, double speed, double heading)
        {
            Aircraft = aircraft;
            TimeStamp = timeStamp;
            GeoCoordinate = new GeoCoordinate(latitude, longitude, altitude, 0, 0, speed, heading);
        }

        public PositionUpdate(string aircraft, double latitude, double longitude) {
            Aircraft = aircraft;
            TimeStamp = DateTime.UtcNow;
            GeoCoordinate = new GeoCoordinate(latitude, longitude);
        }

        public string Aircraft { get; }

        public DateTime TimeStamp { get; }

        public GeoCoordinate GeoCoordinate { get; internal set; }

        public double Latitude => GeoCoordinate.Latitude;
        public double Longitude => GeoCoordinate.Longitude;
        public double Altitude => GeoCoordinate.Altitude;
        public double Speed => GeoCoordinate.Speed;
        public double Heading => GeoCoordinate.Course;
    }
}
