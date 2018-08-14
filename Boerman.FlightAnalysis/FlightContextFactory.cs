using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Boerman.FlightAnalysis.Models;

namespace Boerman.FlightAnalysis
{
    /// <summary>
    /// The FlightContextFactory is a singleton instance capable of handling data inputs from multiple aircraft and 
    /// manages multiple FlightContext instances based on the provided data.
    /// </summary>
    public class FlightContextFactory
    {
        private readonly ConcurrentDictionary<string, FlightContext> _flightContextDictionary =
            new ConcurrentDictionary<string, FlightContext>();
        
        internal readonly Options Options;

        /// <summary>
        /// The constructor for the FlightContextFactory.
        /// </summary>
        /// <param name="options"></param>
        public FlightContextFactory(Options options = null)
        {
            Options = options ?? new Options();

            // Start a timer to remove outtimed context instances.
            new Timer
            {
                Enabled = true,
                Interval = 10000
            }.Elapsed += TimerOnElapsed;
        }

        public FlightContextFactory(IEnumerable<FlightMetadata> metadata, Options options = null) {
            foreach (var flight in metadata) {
                EnsureContextAvailable(flight);
            }

            Options = options ?? new Options();

            // Start a timer to remove outtimed context instances.
            new Timer
            {
                Enabled = true,
                Interval = 10000
            }.Elapsed += TimerOnElapsed;
        }

        public IEnumerable<string> TrackedAircraft
        {
            get
            {
                return _flightContextDictionary.Select(q => q.Key);
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var contextsToRemove =
                _flightContextDictionary
                    .Where( q => q.Value.LastActive < DateTime.UtcNow.Add(-Options.ContextExpiration))
                    .Select(q => q.Key);

            foreach (var contextId in contextsToRemove)
            {
                _flightContextDictionary.TryRemove(contextId, out FlightContext context);

                try
                {
                    OnContextDispose?.Invoke(this, new OnContextDisposedEventArgs(context));
                } catch { }
            }
        }

        /// <summary>
        /// Queue a position update on the FlightContextFactory. The FlightContextFactory will handle further 
        /// processing of this position update.
        /// </summary>
        /// <param name="positionUpdate">The position update to queue</param>
        public void Enqueue(PositionUpdate positionUpdate)
        {
            if (String.IsNullOrWhiteSpace(positionUpdate?.Aircraft)) return;

            EnsureContextAvailable(positionUpdate.Aircraft);
            
            if (_flightContextDictionary.TryGetValue(positionUpdate.Aircraft, out var flightContext))
                flightContext.Enqueue(positionUpdate);
        }

        /// <summary>
        /// Queue a collection of position updates on the FlightContextFactory. The FlightContextFactory will handle
        /// further processing of these position updates.
        /// </summary>
        /// <param name="positionUpdates">The position updates to queue</param>
        public void Enqueue(IEnumerable<PositionUpdate> positionUpdates)
        {
            if (positionUpdates == null) return;

            var updatesByAircraft = positionUpdates
                .Where(q => !String.IsNullOrWhiteSpace(q?.Aircraft))
                .GroupBy(q => q.Aircraft);

            // Group the data by aircraft
            foreach (var updates in updatesByAircraft)
            {
                EnsureContextAvailable(updates.Key);
                
                if (_flightContextDictionary.TryGetValue(updates.Key, out var flightContext))
                    flightContext.Enqueue(updates);
            }
        }

        /// <summary>
        /// The attach method can be used to add an already existing context instance to this factory.
        /// This method will overwrite any FlightContext instance with the same aircraft identifier already
        /// tracked by this FlightContextFactory.
        /// </summary>
        /// <param name="context"></param>
        public void Attach(FlightContext context)
        {
            _flightContextDictionary.TryRemove(context.AircraftId, out _);

            SubscribeContextEventHandlers(context);

            _flightContextDictionary.TryAdd(context.AircraftId, context);
        }
        
        /// <summary>
        /// Checks whether there is a context available for the aircraft which will be processed.
        /// </summary>
        /// <param name="aircraft"></param>
        private void EnsureContextAvailable(string aircraft)
        {
            if (_flightContextDictionary.ContainsKey(aircraft)) return;

            var context = new FlightContext(aircraft);
            SubscribeContextEventHandlers(context);

            _flightContextDictionary.TryAdd(aircraft, context);
        }

        /// <summary>
        /// Checks whether there's a context available based on the metadata provided.
        /// </summary>
        /// <param name="metadata"></param>
        private void EnsureContextAvailable(FlightMetadata metadata)
        {
            if (metadata?.Aircraft == null || _flightContextDictionary.ContainsKey(metadata.Aircraft)) return;

            var context = new FlightContext(metadata.Flight);
            SubscribeContextEventHandlers(context);

            _flightContextDictionary.TryAdd(metadata.Aircraft, context);
        }

        private void SubscribeContextEventHandlers(FlightContext context) {
            // Subscribe to the events so we can propagate 'em via the factory
            context.OnTakeoff += (sender, args) => OnTakeoff?.Invoke(sender, args);
            context.OnLanding += (sender, args) => OnLanding?.Invoke(sender, args);
            context.OnRadarContact += (sender, args) => OnRadarContact?.Invoke(sender, args);
            context.OnCompletedWithErrors += (sender, args) => OnCompletedWithErrors?.Invoke(sender, args);
        }

        /// <summary>
        /// The OnTakeoff event will fire once a takeoff is detected. Please note that the events from individual 
        /// FlightContext instances will be propagated through this event handler.
        /// </summary>
        public event EventHandler<OnTakeoffEventArgs> OnTakeoff;

        /// <summary>
        /// The OnLanding event will fire once a landing is detected. Please note that the events from individual 
        /// FlightContext instances will be propagated through this event handler.
        /// </summary>
        public event EventHandler<OnLandingEventArgs> OnLanding;

        /// <summary>
        /// The OnRadarContact event will fire when a takeoff has not been recorded but an aircraft is mid flight.
        /// Please note that the events from individual FlightContext instances will be propagated through this event
        /// handler.
        /// </summary>
        public event EventHandler<OnRadarContactEventArgs> OnRadarContact;

        /// <summary>
        /// The OnCompletedWithErrors event will fire when flight processing has been completed but some errors have 
        /// been detected (For example destination airfield could not be found). Please note that events from 
        /// individual FlightContext instances will be propagated through this event handler.
        /// </summary>
        public event EventHandler<OnCompletedWithErrorsEventArgs> OnCompletedWithErrors;

        /// <summary>
        /// The OnContextDispose event will fire when a specific FlightContext instance is being disposed. Disposal of
        /// instances will happen if there is no activity for a specific time period.
        /// </summary>
        public event EventHandler<OnContextDisposedEventArgs> OnContextDispose;
    }
}
