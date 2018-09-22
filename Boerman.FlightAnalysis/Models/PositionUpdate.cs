using System;
using RBush;

namespace Boerman.FlightAnalysis.Models
{
    // ToDo: Get this into a readonly struct for performance reasons
    public class PositionUpdate : ISpatialData
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
            GeoCoordinate = new GeoCoordinate(latitude, longitude, altitude, Double.NaN, Double.NaN, speed, heading);

            _envelope = new Envelope(
                GeoCoordinate.Latitude,
                GeoCoordinate.Longitude,
                GeoCoordinate.Latitude,
                GeoCoordinate.Longitude);
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

        /// <summary>
        /// The <see cref="Envelope"/> to show the position of this aircraft. Used for placing this entry in the R-Tree.
        ///
        /// Feel free to clear this object when this instance is not used in any R-Tree anymore in order to save memory.
        /// </summary>
        internal Envelope _envelope;

        public string Aircraft { get; }

        public DateTime TimeStamp { get; }

        public GeoCoordinate GeoCoordinate { get; internal set; }
        
        public double Latitude => GeoCoordinate.Latitude;
        public double Longitude => GeoCoordinate.Longitude;
        public double Altitude => GeoCoordinate.Altitude;
        public double Speed => GeoCoordinate.Speed;
        public double Heading => GeoCoordinate.Course;

        // ToDo/Discussion: Shall we move this to the GeoCoordinate class?
        public ref readonly Envelope Envelope => ref _envelope;
    }
}
