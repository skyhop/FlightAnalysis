namespace Boerman.FlightAnalysis.Models
{
    public class OnContextDisposedEventArgs
    {
        public OnContextDisposedEventArgs(FlightContext context)
        {
            Context = context;
        }

        public FlightContext Context { get; set; }
    }
}
