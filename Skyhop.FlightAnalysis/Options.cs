using System;

namespace Skyhop.FlightAnalysis
{
    // For future me, or if you wonder why these options are not auto properties on the
    // FlightContextFactory. I only want to be able to set these props during intialization
    // to prevent unexpected behaviour.
    public class Options
    {
        /// <summary>
        /// Provide the options for the <see cref="FlightContextFactory" through this constructor. />
        /// </summary>
        /// <param name="contextExpiration">Set the expiration time of a <seealso cref="FlightContext"/>.</param>
        /// <param name="minifyMemoryPressure">Minify the memory pressure of the FlightContextFactory. This is done by only keeping a small selection of the last posisition updates in-memory. Note that the implication is that the <seealso cref="Flight"/> propery does not contain full flight information in the callbacks.</param>
        public Options(TimeSpan? contextExpiration, bool minifyMemoryPressure)
        {
            ContextExpiration = contextExpiration ?? TimeSpan.FromHours(1);
            MinifyMemoryPressure = minifyMemoryPressure;
        }

        public Options() : this(null, false) { }
        public Options(bool minifyMemoryPressure) : this(null, minifyMemoryPressure) { }
        public Options(TimeSpan? contextExpiration) : this(contextExpiration, false) { }

        private TimeSpan _contextExpiration;
        public TimeSpan ContextExpiration
        {
            get
            {
                return _contextExpiration;
            }
            private set
            {
                // Make sure the value is always positive, just to prevent some stupid bugs.
                _contextExpiration = value.Ticks < 0
                    ? -value
                    : value;
            }
        }

        public bool MinifyMemoryPressure { get; private set; }
    }
}
