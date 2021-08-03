namespace Skyhop.FlightAnalysis.Models
{
    public class OnLaunchCompletedEventArgs
    {
        public OnLaunchCompletedEventArgs(Flight flight)
        {
            Flight = flight;
        }

        public Flight Flight { get; }
    }
}