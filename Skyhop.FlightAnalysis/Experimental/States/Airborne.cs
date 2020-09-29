namespace Skyhop.FlightAnalysis.Experimental
{
    internal static partial class MachineStates
    {
        internal static void Airborne(this FlightContext context)
        {
            if (context.CurrentPosition.Altitude < 300)
            {
                context.StateMachine.Fire(FlightContext.Trigger.Landing);
            }
        }
    }
}
