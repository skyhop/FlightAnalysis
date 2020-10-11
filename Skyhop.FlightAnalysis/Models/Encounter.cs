using System;

namespace Skyhop.FlightAnalysis.Models
{
    public class Encounter
    {
        public string Aircraft { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public EncounterType Type { get; set; }
    }

    public enum EncounterType
    {
        Nearby,
        Towing,
        Tug
    }
}
