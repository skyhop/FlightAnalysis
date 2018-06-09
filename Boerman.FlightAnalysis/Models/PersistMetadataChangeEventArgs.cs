namespace Boerman.FlightAnalysis.Models
{
    public class PersistMetadataChangeEventArgs
    {
        public PersistMetadataChangeEventArgs(Flight flight)
        {
            Flight = flight.ViewModel;
        }

        public FlightViewModel Flight { get; }
    }
}
