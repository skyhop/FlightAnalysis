using System;

namespace Skyhop.FlightAnalysis.Models
{
    [Flags]
    public enum LaunchMethods
    {
        None = 0,
        Unknown = 1,
        Winch = 2,
        Aerotow = 4,
        Self = 8
    }
}
