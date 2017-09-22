using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Boerman.Aeronautics.FlightAnalysis.Models;

namespace Boerman.Aeronautics.FlightAnalysis
{
    /// <summary>
    /// The FlightContextFactory is a singleton instance capable of handling data inputs from multiple aircraft and 
    /// manages multiple FlightContext instances based on the provided data.
    /// </summary>
    public class FlightContextFactory
    {
        /// <summary>
        /// Retrieves the instance of the FlightContextFactory
        /// </summary>
        public static FlightContextFactory Instance => Lazy.Value;

        private static readonly Lazy<FlightContextFactory> Lazy =
            new Lazy<FlightContextFactory>(() => new FlightContextFactory());

        private readonly ConcurrentDictionary<string, FlightContext> _flightContextDictionary =
            new ConcurrentDictionary<string, FlightContext>();
        
        private readonly Options _options;
        
        /// <summary>
        /// The constructor for the FlightContextFactory.
        /// </summary>
        /// <param name="options"></param>
        private FlightContextFactory(Options options = null)
        {
            _options = options;

            // Start a timer to remove outtimed context instances.
            new Timer
            {
                Enabled = true,
                Interval = 10000
            }.Elapsed += TimerOnElapsed;
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var contextsToRemove =
                _flightContextDictionary
                    .Where( q => q.Value.LastActive < DateTime.UtcNow.Add(-_options.ContextExpiration))
                    .Select(q => q.Key);

            foreach (var contextId in contextsToRemove)
            {
                _flightContextDictionary.TryRemove(contextId, out FlightContext context);
                OnContextDispose?.Invoke(context, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Queue a position update on the FlightContextFactory. The FlightContextFactory will handle further 
        /// processing of this position update.
        /// </summary>
        /// <param name="positionUpdate">The position update to queue</param>
        public void Enqueue(PositionUpdate positionUpdate)
        {
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
            var updatesByAircraft = positionUpdates.GroupBy(q => q.Aircraft);

            // Group the data by aircraft
            foreach (var updates in updatesByAircraft)
            {
                EnsureContextAvailable(updates.Key);
                
                if (_flightContextDictionary.TryGetValue(updates.Key, out var flightContext))
                    flightContext.Enqueue(updates);
            }
        }
        
        /// <summary>
        /// Checks whether there is a context available for the aircraft which will be processed.
        /// </summary>
        /// <param name="aircraft"></param>
        private void EnsureContextAvailable(string aircraft)
        {
            if (_flightContextDictionary.ContainsKey(aircraft)) return;

            var context = new FlightContext(aircraft);

            // Subscribe to the events so we can propagate 'em via the factory
            context.OnTakeoff += (sender, args) => OnTakeoff?.Invoke(context, args);
            context.OnLanding += (sender, args) => OnLanding?.Invoke(sender, args);
            context.OnCompletedWithErrors += (sender, args) => OnCompletedWithErrors?.Invoke(sender, args);

            _flightContextDictionary.TryAdd(aircraft, context);
        }

        /// <summary>
        /// The OnTakeoff event will fire once a takeoff is detected. For further information check the sender object
        /// which is of the FlightContext type. Please note that the events from individual FlightContext instances 
        /// will be propagated through this event handler.
        /// </summary>
        public event EventHandler<OnTakeoffEventArgs> OnTakeoff;

        /// <summary>
        /// The OnLanding event will fire once a landing is detected. For further information check the sender object
        /// which is of the FlightContext type. Please note that the events from individual FlightContext instances
        /// will be propagated through this event handler.
        /// </summary>
        public event EventHandler<OnLandingEventArgs> OnLanding;

        /// <summary>
        /// The OnCompletedWithErrors event will fire when flight processing has been completed but some errors have 
        /// been detected. (For example destination airfield could not be found) For further information check the
        /// sender object which is of the FlightContext type. Please note that events from individual FlightContext
        /// instances will be propagated through this event handler.
        /// </summary>
        public event EventHandler<OnCompletedWithErrorsEventArgs> OnCompletedWithErrors;

        /// <summary>
        /// The OnContextDispose event will fire when a specific FlightContext instance is being disposed. Disposal of
        /// instances will happen if there is no activity for a specific time period.
        /// </summary>
        public event EventHandler OnContextDispose;
    }
}
