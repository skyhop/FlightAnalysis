namespace Boerman.Aeronautics.FlightAnalysis.Models
{
    public class OnCompletedWithErrorsEventArgs
    {
        public OnCompletedWithErrorsEventArgs(Flight flight)
        {
            Flight = flight;
        }

        public Flight Flight { get; }
    }
}