using System;

namespace Boerman.FlightAnalysis.Models
{
    public class PositionUpdate
    {
        /// <summary>
        /// Create a new instance of the <see cref="PositionUpdate"/> class using all the available parameters.
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="timeStamp"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="altitude"></param>
        /// <param name="speed"></param>
        /// <param name="heading"></param>
        public PositionUpdate(string aircraft, DateTime timeStamp, double latitude, double longitude, double altitude, double speed, double heading)
        {
            Aircraft = aircraft;
            TimeStamp = timeStamp;
            GeoCoordinate = new GeoCoordinate(latitude, longitude, altitude, 0, 0, speed, heading);
        }

        /// <summary>
        /// Create a new instance of the <see cref="PositionUpdate"/> class.
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="timeStamp"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        public PositionUpdate(string aircraft, DateTime timeStamp, double latitude, double longitude)
        {
            Aircraft = aircraft;
            TimeStamp = timeStamp;
            GeoCoordinate = new GeoCoordinate(latitude, longitude);
        }

        /// <summary>
        /// Create a new instance of the <see cref="PositionUpdate"/> class with a minimal set of data.
        ///
        /// The timestamp associated with this object will be the time of instantiation.
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
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
