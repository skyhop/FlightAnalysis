using Skyhop.FlightAnalysis.Internal;
using System.Linq;

namespace Skyhop.FlightAnalysis
{
    internal static partial class MachineStates
    {
        internal static void ResolveAerotowStatus(this FlightContext context)
        {

            var nearbyAircraft = context.Options.NearbyAircraftAccessor?.Invoke((
                coordinate: context.Flight.PositionUpdates.Last().Location,
                distance: 200));

            if (nearbyAircraft != null && nearbyAircraft.Any())
            {
                var towStatus = Geo.AircraftRelation.None;
                FlightContext otherContext = null;

                foreach (var aircraft in nearbyAircraft)
                {
                    var status = context.DetermineTowStatus(aircraft);

                    if (status != Geo.AircraftRelation.None)
                    {
                        otherContext = aircraft;
                        towStatus = status;
                        break;
                    }
                }

                if (towStatus != Geo.AircraftRelation.None && otherContext != null)
                {
                    // ToDo: Update the status, and continue tracking the tow, point for point
                    context.Flight.LaunchMethod = Models.LaunchMethods.Aerotow;
                } else
                {
                    context.Flight.LaunchMethod &= ~Models.LaunchMethods.Aerotow;
                }
            }
        }
    }
}
