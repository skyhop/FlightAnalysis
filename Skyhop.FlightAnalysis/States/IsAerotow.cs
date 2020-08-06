using Skyhop.FlightAnalysis.Internal;
using System.Linq;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        private static (FlightContext context, Geo.AircraftRelation status)? IsAerotow(this FlightContext context)
        {
            var nearbyAircraft = context.Options.NearbyAircraftAccessor?.Invoke((
                coordinate: context.Flight.PositionUpdates.Last().Location,
                distance: 200));

            if (nearbyAircraft != null && nearbyAircraft.Any())
            {
                foreach (var aircraft in nearbyAircraft)
                {
                    var status = context.DetermineTowStatus(aircraft);

                    if (status != Geo.AircraftRelation.None)
                    {
                        return (aircraft, status);
                    }
                }
            }

            return null;
        }
    }
}
