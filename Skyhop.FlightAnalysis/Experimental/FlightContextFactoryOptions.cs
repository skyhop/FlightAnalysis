using System;

namespace Skyhop.FlightAnalysis.Experimental
{
    public class FlightContextFactoryOptions : Options
    {
        public TimeSpan ContextExpiration { get; set; } = TimeSpan.FromHours(1);
    }
}
