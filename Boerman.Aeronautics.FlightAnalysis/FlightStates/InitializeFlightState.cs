using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Boerman.Aeronautics.FlightAnalysis.Models;

namespace Boerman.Aeronautics.FlightAnalysis.FlightStates
{
    /// <summary>
    /// The InitializeFlightState class is being used to clear the FlightContext to allow for further processing after 
    /// a single flight has been processed.
    /// </summary>
    public class InitializeFlightState : FlightState
    {
        public InitializeFlightState(FlightContext context) : base(context)
        {
        }

        public override async Task Run()
        {
            Context.LatestTimeStamp = DateTime.MinValue;

            Context.Flight = new Flight
            {
                Aircraft = Context.AircraftId
            };

            Context.PositionUpdates = new List<PositionUpdate>();
        }
    }
}
