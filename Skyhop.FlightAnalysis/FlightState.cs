using Boerman.Core.State;

namespace Skyhop.FlightAnalysis
{
    /// <summary>
    /// The FlightState object is the base class used for the processing steps used in the FlightContext
    /// </summary>
    public abstract class FlightState : BaseState
    {
        /// <summary>
        /// Reference to the FlightContext instance which invokes this FlightState.
        /// </summary>
        public new FlightContext Context => base.Context as FlightContext;

        protected FlightState(FlightContext context) : base(context)
        {

        }
    }
}
