using System.Linq;
using System.Runtime;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        internal static void TrackAerotow(this FlightContext context) {
            // ToDo: Think about the possibility of a paired landing.

            var target = context.Flight.Encounters
                .FirstOrDefault(q => q.Type == Models.EncounterType.Tow
                    || q.Type == Models.EncounterType.Tug);

            if (target == null)
            {
                // Note of caution; this situation should ideally never happen. If it does it would be a design flaw in the state machine?
                context.StateMachine.Fire(FlightContext.Trigger.LaunchCompleted);
                return;
            }

            // ToDo: Check separation


        }
    }
}
