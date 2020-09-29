using Skyhop.FlightAnalysis.Models;
using System;

namespace Skyhop.FlightAnalysis.Experimental
{
    internal static partial class MachineStates
    {
        internal static void Initialize(this FlightContext context)
        {
            context.LatestTimeStamp = DateTime.MinValue;

            context.Flight = new Flight
            {
                Aircraft = context.Options.AircraftId
            };

            context.StateMachine.Fire(FlightContext.Trigger.Next);
        }
    }
}
