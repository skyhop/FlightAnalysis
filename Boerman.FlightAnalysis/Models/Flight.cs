using System;
using System.Collections.Generic;

namespace Boerman.FlightAnalysis.Models
{
    public class Flight
    {
        public Flight() {
            
        }

        public Flight (FlightViewModel viewModel) {
            Aircraft = viewModel.Aircraft;
            LastSeen = viewModel.LastSeen;
            StartTime = viewModel.StartTime;
            DepartureHeading = viewModel.DepartureHeading;
            DepartureLocation = viewModel.DepartureLocation;
            DepartureInfoFound = viewModel.DepartureInfoFound;
            EndTime = viewModel.EndTime;
            ArrivalHeading = viewModel.ArrivalHeading;
            ArrivalInfoFound = viewModel.ArrivalInfoFound;
            PositionUpdates = viewModel.PositionUpdates;
        }

        public string Aircraft { get; internal set; }

        public DateTime? LastSeen { get; internal set; }

        public DateTime? StartTime { get; internal set; }
        public short DepartureHeading { get; internal set; }
        public GeoCoordinate DepartureLocation { get; internal set; }
        public bool? DepartureInfoFound { get; internal set; }

        public DateTime? EndTime { get; internal set; }
        public short ArrivalHeading { get; internal set; }
        public GeoCoordinate ArrivalLocation { get; internal set; }
        public bool? ArrivalInfoFound { get; internal set; }

        public ICollection<PositionUpdate> PositionUpdates { get; internal set; }

        public FlightViewModel ViewModel => new FlightViewModel(this);
    }
}
