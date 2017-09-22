namespace Boerman.Aeronautics.FlightAnalysis.Models
{
    public class OnTakeoffEventArgs
    {
        public OnTakeoffEventArgs(Flight flight)
        {
            Flight = flight;
        }

        public Flight Flight { get; }
    }
}