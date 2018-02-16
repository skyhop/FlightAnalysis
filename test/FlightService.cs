using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Boerman.FlightAnalysis;
using Boerman.FlightAnalysis.Models;
using Microsoft.Extensions.Hosting;

namespace SkyHop.Core.Services
{
    /// <summary>
    /// The FlightService processes position information in order to collect
    /// general flight metadata.
    /// </summary>
    public class FlightService : IHostedService
    {
        private FlightContextFactory factory = new FlightContextFactory();

        public FlightService()
        {
            factory.OnCompletedWithErrors += Factory_OnCompletedWithErrors;
            factory.OnContextDispose += Factory_OnContextDispose;
            factory.OnLanding += Factory_OnLanding;
            factory.OnRadarContact += Factory_OnRadarContact;
            factory.OnTakeoff += Factory_OnTakeoff;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Enqueue(PositionUpdate positionUpdate)
        {
            factory.Enqueue(positionUpdate);
        }

        public void Enqueue(IEnumerable<PositionUpdate> positionUpdates)
        {
            factory.Enqueue(positionUpdates);
        }

        void Factory_OnCompletedWithErrors(object sender, Boerman.FlightAnalysis.Models.OnCompletedWithErrorsEventArgs e)
        {
            Debug.WriteLine($"{nameof(FlightService)}: Flight could not be fully resolved {e.Flight.Aircraft}");
        }

        void Factory_OnContextDispose(object sender, EventArgs e)
        {
            Debug.WriteLine($"{nameof(FlightService)}: Context disposed");
        }

        void Factory_OnLanding(object sender, Boerman.FlightAnalysis.Models.OnLandingEventArgs e)
        {
            Debug.WriteLine($"{nameof(FlightService)}: {e.Flight.Aircraft} landed");
        }

        void Factory_OnRadarContact(object sender, Boerman.FlightAnalysis.Models.OnRadarContactEventArgs e)
        {
            Debug.WriteLine($"{nameof(FlightService)}: Radar contact with {e.Flight.Aircraft}");
        }

        void Factory_OnTakeoff(object sender, Boerman.FlightAnalysis.Models.OnTakeoffEventArgs e)
        {
            Debug.WriteLine($"{nameof(FlightService)}: {e.Flight.Aircraft} took off");
        }
    }
}
