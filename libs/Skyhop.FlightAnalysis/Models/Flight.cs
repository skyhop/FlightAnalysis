using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using static Skyhop.FlightAnalysis.FlightContext;

namespace Skyhop.FlightAnalysis.Models
{
    /*
     * ToDo: As this model is mainly used for processing the actual flight,
     * try to abstract this class away from the event handlers.
     * 
     * There is no need in immutability of events when the model is already
     * copied.
     * 
     */

    /// <summary>
    /// The Flight class is the workhorse for the FlightAnalysis library. 
    /// </summary>
    public class Flight
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Aircraft { get; set; }

        public string DeviceId { get; internal set; }

        public AircraftType AircraftType { get; internal set; }

        public AddressType AddressType { get; internal set; }

        public DateTime? DepartureTime { get; set; }
        public short DepartureHeading { get; set; }
        public Point DepartureLocation { get; set; }
        public bool? DepartureInfoFound { get; set; }

        public LaunchMethods LaunchMethod { get; set; }
        public DateTime? LaunchFinished { get; set; }

        public DateTime? ArrivalTime { get; set; }
        public short ArrivalHeading { get; set; }
        public Point ArrivalLocation { get; set; }
        public State State { get; set; }

        /// <summary>
        /// Indicate whether the information depicted is calculated (true) or theorized (false)
        /// </summary>
        public bool? ArrivalInfoFound { get; set; }

        public List<PositionUpdate> PositionUpdates { get; } = new List<PositionUpdate>();
        public List<Encounter> Encounters { get; } = new List<Encounter>();

        public bool Completed => (DepartureInfoFound != null || DepartureTime != null) && (ArrivalInfoFound != null || ArrivalTime != null);
    }
}
