using System;

namespace Boerman.Aeronautics.FlightAnalysis
{
    public class Options
    {
        public TimeSpan CacheExpiration { get; set; }

        private TimeSpan _contextExpiration;
        public TimeSpan ContextExpiration
        {
            get
            {
                return _contextExpiration;
            }
            set {
                // Make sure the value is always positive, just to prevent some stupid bugs.
                _contextExpiration = value.Ticks < 0 
                    ? TimeSpan.FromTicks(-value.Ticks) 
                    : value;
            }
        }
    }
}
