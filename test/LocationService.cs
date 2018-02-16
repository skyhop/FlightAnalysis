using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Boerman.AprsClient;
using Microsoft.Extensions.Hosting;
using Timer = System.Timers.Timer;

namespace SkyHop.Core.Services
{
    /// <summary>
    /// The LocationService collects position updates from the aircraft which
    /// are being tracked.
    /// </summary>
    public class LocationService : IHostedService
    {
        public Listener AprsClient = new Listener(new Config
        {
            Callsign = @"SHCOGN",
            Password = "",
            Uri = "aprs.glidernet.org",
            UseOgnAdditives = false,
            Port = 10152
        });

        private FlightService flightService;

        private Timer timer = new Timer();

        private int receivedPackets = 0;

        public LocationService(FlightService flightService)
        {
            this.flightService = flightService;

            AprsClient.Connected += AprsClient_Connected;
            AprsClient.Disconnected += AprsClient_Disconnected;
            AprsClient.PacketReceived += AprsClient_PacketReceived;

            // Initialize the timer for some metric retrieval stuff ya know
            timer.Interval = 10000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            Debug.WriteLine($"{nameof(LocationService)}: initialized");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await AprsClient.Open();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            AprsClient.Stop();
        }

        void AprsClient_Connected(object sender, Boerman.Networking.ConnectedEventArgs e)
        {
            Debug.WriteLine($"{nameof(LocationService)}: listener connected");
        }

        void AprsClient_Disconnected(object sender, Boerman.Networking.DisconnectedEventArgs e)
        {
            // ToDo: Check default behaviour of the listener
            Debug.WriteLine($"{nameof(LocationService)}: listener disconnected");

        }

        void AprsClient_PacketReceived(object sender, Boerman.AprsClient.Models.PacketReceivedEventArgs e)
        {
            // ToDo: Store in the database
            // ToDo: Continue processing in the flight analysis tooling

            receivedPackets++;
            flightService.Enqueue(e.AprsMessage.ToPositionUpdate());
        }

        void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Debug.WriteLine($"{nameof(LocationService)}: {receivedPackets} messages received in the last 10 seconds");
            receivedPackets = 0;
        }
    }
}
