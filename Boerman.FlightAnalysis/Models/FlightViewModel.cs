using System;
using System.Collections.Generic;

namespace Boerman.FlightAnalysis.Models
{
    public class FlightViewModel
    {
        public FlightViewModel() {
            Id = Guid.NewGuid();
        }

        public FlightViewModel(Flight flight) {
            Id = flight.Id;
            Aircraft = flight.Aircraft;
            LastSeen = flight.LastSeen;
            StartTime = flight.StartTime;
            DepartureHeading = flight.DepartureHeading;
            DepartureLocation = flight.DepartureLocation;
            DepartureInfoFound = flight.DepartureInfoFound;
            EndTime = flight.EndTime;
            ArrivalHeading = flight.ArrivalHeading;
            ArrivalLocation = flight.ArrivalLocation;
            ArrivalInfoFound = flight.ArrivalInfoFound;
            PositionUpdates = flight.PositionUpdates;
        }

        public Guid Id { get; set; }

        public string Aircraft { get; set; }

        public DateTime? LastSeen { get; set; }

        public DateTime? StartTime { get; set; }
        public short DepartureHeading { get; set; }
        public GeoCoordinate DepartureLocation { get; set; }
        public bool? DepartureInfoFound { get; set; }

        public DateTime? EndTime { get; set; }
        public short ArrivalHeading { get; set; }
        public GeoCoordinate ArrivalLocation { get; set; }
        public bool? ArrivalInfoFound { get; set; }

        public ICollection<PositionUpdate> PositionUpdates { get; set; }

        public Flight Flight => new Flight(this);
    }
}
