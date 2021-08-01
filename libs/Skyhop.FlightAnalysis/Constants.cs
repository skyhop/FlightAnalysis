namespace Skyhop.FlightAnalysis
{
    public static class Constants
    {
        public const int ArrivalHeight = 350;
        public const int ArrivalTimeout = 30;
        public const int RunwayQueryRadius = 10;

#if DEBUG
#pragma warning disable CA1707 // Identifiers should not contain underscores
        public const string DEBUG__FlarmId = "ICA484977";
#pragma warning restore CA1707 // Identifiers should not contain underscores
#endif
    }
}
