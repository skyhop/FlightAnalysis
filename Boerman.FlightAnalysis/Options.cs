using System;

namespace Boerman.FlightAnalysis
{
    public class Options
    {
        public Options(TimeSpan? contextExpiration = null, TimeSpan? cacheExpiration = null) {
            ContextExpiration = contextExpiration ?? TimeSpan.FromHours(1);
            CacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
        }

        public TimeSpan CacheExpiration { get; private set; }

        private TimeSpan _contextExpiration;
        public TimeSpan ContextExpiration
        {
            get
            {
                return _contextExpiration;
            }
            private set {
                // Make sure the value is always positive, just to prevent some stupid bugs.
                _contextExpiration = value.Ticks < 0 
                    ? TimeSpan.FromTicks(-value.Ticks) 
                    : value;
            }
        }
    }
}
