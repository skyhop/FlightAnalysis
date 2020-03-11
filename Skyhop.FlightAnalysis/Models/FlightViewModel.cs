using System;
using System.Collections.Generic;

namespace Skyhop.FlightAnalysis.Models
{
    [Serializable]
    public class FlightViewModel : FlightMetadata
    {
        public FlightViewModel()
        {
        }

        public FlightViewModel(Flight flight)
        {
            PositionUpdates = flight.PositionUpdates;
        }

        public List<PositionUpdate> PositionUpdates { get; set; }
    }
}
