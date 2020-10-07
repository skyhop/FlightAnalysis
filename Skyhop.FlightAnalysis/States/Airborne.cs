namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        internal static void Airborne(this FlightContext context)
        {
            if (context.CurrentPosition.Altitude < Constants.ArrivalHeight)
            {
                context.StateMachine.Fire(FlightContext.Trigger.Landing);
            }
        }
    }
}
