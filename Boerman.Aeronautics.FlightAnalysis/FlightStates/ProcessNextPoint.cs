using System.Threading.Tasks;
using Boerman.Aeronautics.FlightAnalysis.Models;

namespace Boerman.Aeronautics.FlightAnalysis.FlightStates
{
    /// <summary>
    /// The ProcessNextPoint state is being invoked to start processing of the next available data point.
    /// </summary>
    public class ProcessNextPoint : FlightState
    {
        public ProcessNextPoint(FlightContext context) : base(context)
        {
        }

        public override async Task Run()
        {
            if (Context.Flight.EndTime != null)
            {
                Context.QueueState(typeof(InitializeFlightState));
                Context.QueueState(typeof(ProcessNextPoint));
            }
            else
            {
                if (Context.Heap.IsEmpty) return;

                var id = Context.Heap.DeleteMin();

                Context.Data.TryRemove(id, out PositionUpdate positionUpdate);

                if (positionUpdate == null)
                {
                    Context.QueueState(typeof(ProcessNextPoint));
                    return;
                }
                
                Context.PositionUpdates.Add(positionUpdate);
                Context.QueueState(typeof(DetermineFlightState));
            }
        }
    }
}
