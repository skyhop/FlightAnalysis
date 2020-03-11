using System;
using System.Threading.Tasks;
using Skyhop.FlightAnalysis.Models;

namespace Skyhop.FlightAnalysis.FlightStates
{
    /// <summary>
    /// The InitializeFlightState class is being used to clear the FlightContext to allow for further processing after 
    /// a single flight has been processed.
    /// </summary>
    internal class InitializeFlightState : FlightState
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
        }
    }
}
