using System;
using System.Collections.Generic;

namespace Boerman.FlightAnalysis.Models
{
    [Serializable]
    public class FlightViewModel : FlightMetadata
    {
        public FlightViewModel() {
        }

        public FlightViewModel(Flight flight) {
            PositionUpdates = flight.PositionUpdates;
        }

        public ICollection<PositionUpdate> PositionUpdates { get; set; }
    }
}
