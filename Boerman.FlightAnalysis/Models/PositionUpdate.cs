using System;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace Boerman.FlightAnalysis.Models
{
    // ToDo: Get this into a readonly struct for performance reasons
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
        [JsonConstructor]
        public PositionUpdate(string aircraft, DateTime timeStamp, double latitude, double longitude, double altitude, double speed, double heading)
        {
            Aircraft = aircraft;
            TimeStamp = timeStamp;
            //Location = new Point(latitude, longitude, altitude);
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            Speed = speed;
            Heading = heading;

            // ToDo: Figure something out to implement the RBush again
        }

        /// <summary>
        /// Create a new instance of the <see cref="PositionUpdate"/> class.
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="timeStamp"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        public PositionUpdate(string aircraft, DateTime timeStamp, double latitude, double longitude) : this(aircraft, timeStamp, latitude, longitude, Double.NaN, Double.NaN, Double.NaN) { }

        /// <summary>
        /// Create a new instance of the <see cref="PositionUpdate"/> class with a minimal set of data.
        ///
        /// The timestamp associated with this object will be the time of instantiation.
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        public PositionUpdate(string aircraft, double latitude, double longitude) : this(aircraft, DateTime.UtcNow, latitude, longitude, Double.NaN, Double.NaN, Double.NaN) { }
        
        public string Aircraft { get; }

        public DateTime TimeStamp { get; }

        [JsonIgnore]
        public Point Location => new Point(Latitude, Longitude, Altitude);

        public double Latitude { get; }
        public double Longitude { get; }
        public double Altitude { get; }

        // Note that these properties need to be internally assignable for the normalization algorithm to work.
        public double Speed { get; internal set; }
        public double Heading { get; internal set; }
    }
}
