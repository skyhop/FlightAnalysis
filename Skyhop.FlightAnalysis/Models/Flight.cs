using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

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
        public Flight()
        {
            Id = Guid.NewGuid();
            PositionUpdates = new List<PositionUpdate>();
        }

        public Flight(FlightMetadata metadata)
        {
            Id = metadata.Id ?? Guid.NewGuid();
            Aircraft = metadata.Aircraft;
            LastSeen = metadata.LastSeen;
            StartTime = metadata.DepartureTime;
            DepartureHeading = metadata.DepartureHeading;
            DepartureLocation = metadata.DepartureLocation;
            DepartureInfoFound = metadata.DepartureInfoFound;
            EndTime = metadata.ArrivalTime;
            ArrivalHeading = metadata.ArrivalHeading;
            ArrivalInfoFound = metadata.ArrivalInfoFound;
            ArrivalLocation = metadata.ArrivalLocation;

            if (metadata is FlightViewModel)
                PositionUpdates = ((FlightViewModel)metadata).PositionUpdates ?? new List<PositionUpdate>();
            else
                PositionUpdates = new List<PositionUpdate>();
        }

        public Guid Id { get; internal set; }

        public string Aircraft { get; internal set; }

        public DateTime? LastSeen { get; internal set; }

        public DateTime? StartTime { get; internal set; }
        public short DepartureHeading { get; internal set; }
        public Point DepartureLocation { get; internal set; }
        public bool? DepartureInfoFound { get; internal set; }

        public DateTime? EndTime { get; internal set; }
        public short ArrivalHeading { get; internal set; }
        public Point ArrivalLocation { get; internal set; }
        public bool? ArrivalInfoFound { get; internal set; }

        public List<PositionUpdate> PositionUpdates { get; internal set; }

        public FlightViewModel ViewModel => new FlightViewModel(this);
        public FlightMetadata Metadata => new FlightMetadata(this);
    }
}
