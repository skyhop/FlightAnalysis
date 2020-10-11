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

    /// <summary>
    /// The setting in which an encounter happened
    /// </summary>
    public enum EncounterType
    {
        /// <summary>
        /// This aircraft has been nearby
        /// </summary>
        Nearby,
        /// <summary>
        /// This aircraft is on our tow
        /// </summary>
        Tow,
        /// <summary>
        /// This encounter is our tug
        /// </summary>
        Tug
    }
}
