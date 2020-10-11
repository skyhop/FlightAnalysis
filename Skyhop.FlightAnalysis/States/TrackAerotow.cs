using System;
using System.Linq;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        internal static void TrackAerotow(this FlightContext context) {
            // ToDo: Think about the possibility of a paired landing.

            if (context.Options.AircraftAccessor == null) throw new Exception($"Unable to track tow without {nameof(FlightContextFactory)}");

            var target = context.Flight.Encounters
                .FirstOrDefault(q => q.Type == Models.EncounterType.Tow
                    || q.Type == Models.EncounterType.Tug);

            if (target == null)
            {
                // Note of caution; this situation should ideally never happen. If it does it would be a design flaw in the state machine?
                context.StateMachine.Fire(FlightContext.Trigger.LaunchCompleted);
                return;
            }

            var otherContext = context.Options.AircraftAccessor(target.Aircraft);

            var status = context.DetermineTowStatus(otherContext);

            if (status == Internal.Geo.AircraftRelation.None
                || (target.Type == Models.EncounterType.Tow && status != Internal.Geo.AircraftRelation.Towplane)
                || (target.Type == Models.EncounterType.Tug && status != Internal.Geo.AircraftRelation.OnTow))
            {
                target.End = context.CurrentPosition.TimeStamp;
                context.StateMachine.Fire(FlightContext.Trigger.LaunchCompleted);
                context.InvokeOnLaunchCompletedEvent();
                return;
            }
        }
    }
}
