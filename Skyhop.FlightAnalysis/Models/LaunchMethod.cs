using System;

namespace Skyhop.FlightAnalysis.Models
{
    [Flags]
    public enum LaunchMethods
    {
        Unknown = 1,
        Winch = 2,
        Aerotow = 4,
        Self = 8
    }
}
