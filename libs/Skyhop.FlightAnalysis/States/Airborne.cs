using Skyhop.FlightAnalysis.Internal;
using System.Linq;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        internal static void Airborne(this FlightContext context)
        {
            double groundElevation = 0;
            if (context.Options.NearbyRunwayAccessor != null)
            {
                groundElevation = context.Options.NearbyRunwayAccessor(
                    context.CurrentPosition.Location,
                    Constants.RunwayQueryRadius)?
                    .OrderBy(q => q.Sides
                        .Min(w => Geo.DistanceTo(w, context.CurrentPosition.Location))
                    ).FirstOrDefault()
                    ?.Sides
                    .Average(q => q.Z)
                    ?? 0;
            }

            if (context.CurrentPosition.Altitude < (groundElevation + Constants.ArrivalHeight) 
                || context.CurrentPosition.Speed == 0)
            {
                context.StateMachine.Fire(FlightContext.Trigger.Landing);
            }
        }
    }
}
