using NetTopologySuite.Geometries;
using Skyhop.FlightAnalysis.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Timers;

namespace Skyhop.FlightAnalysis.Experimental
{
    /// <summary>
    /// The FlightContextFactory is a singleton instance capable of handling data inputs from multiple aircraft and 
    /// manages multiple FlightContext instances based on the provided data.
    /// </summary>
    public class FlightContextFactory
    {
        // ToDo: Possible performance improvement over here
        private readonly SpatialMap<PositionUpdate> _map = new SpatialMap<PositionUpdate>(
            q => Math.Cos(Math.PI / 180 * q.Location.Y) * 111 * q.Location.X,
            q => q.Location.Y * 111);

        private readonly ConcurrentDictionary<string, FlightContext> _flightContextDictionary =
            new ConcurrentDictionary<string, FlightContext>();

        internal readonly FlightContextFactoryOptions Options = new FlightContextFactoryOptions();

        /// <summary>
        /// The constructor for the FlightContextFactory.
        /// </summary>
        /// <param name="options"></param>
        public FlightContextFactory(Action<FlightContextFactoryOptions> options = default)
        {
            options?.Invoke(Options);

            // Start a timer to remove outtimed context instances.
            new Timer
            {
                Enabled = true,
                Interval = 10000
            }.Elapsed += (sender, args) => TimerOnElapsed();
        }

        public FlightContextFactory(IEnumerable<FlightMetadata> metadata, Action<FlightContextFactoryOptions> options = default)
        {
            foreach (var flight in metadata)
            {
                EnsureContextAvailable(flight);
            }

            options?.Invoke(Options);

            // Start a timer to remove outtimed context instances.
            new Timer
            {
                Enabled = true,
                Interval = 10000
            }.Elapsed += (sender, arguments) => TimerOnElapsed();
        }

        public IEnumerable<string> TrackedAircraft => _flightContextDictionary.Select(q => q.Key);

        private void TimerOnElapsed()
        {
            var contextsToRemove =
                _flightContextDictionary
                    .Where(q => q.Value.LastActive < DateTime.UtcNow.Add(-Options.ContextExpiration))
                    .Select(q => q.Key);

            foreach (var contextId in contextsToRemove)
            {
                _flightContextDictionary.TryRemove(contextId, out FlightContext context);

                var latest = context.Flight.PositionUpdates.LastOrDefault();

                if (latest != null)
                {
                    _map.Remove(latest);
                }

                try
                {
# warning fix before release
                    //OnContextDispose?.Invoke(this, new OnContextDisposedEventArgs(context));
                }
                catch (Exception ex)
                {
                    Trace.Write(ex);
                }
            }
        }

        /// <summary>
        /// Queue a position update on the FlightContextFactory. The FlightContextFactory will handle further 
        /// processing of this position update.
        /// </summary>
        /// <param name="positionUpdate">The position update to queue</param>
        public void Enqueue(PositionUpdate positionUpdate)
        {
            if (positionUpdate == null) return;

            EnsureContextAvailable(positionUpdate.Aircraft);

            if (_flightContextDictionary.TryGetValue(positionUpdate.Aircraft, out var flightContext))
            {
                // Grab the most recent position available
                var previousPoint = flightContext.Flight.PositionUpdates.LastOrDefault();

                flightContext.Enqueue(positionUpdate);

                _map.Add(positionUpdate);

                if (previousPoint != null)
                {
                    _map.Remove(previousPoint);
                }
            }
        }

        /// <summary>
        /// Queue a collection of position updates on the FlightContextFactory. The FlightContextFactory will handle
        /// further processing of these position updates.
        /// </summary>
        /// <param name="positionUpdates">The position updates to queue</param>
        public void Enqueue(IEnumerable<PositionUpdate> positionUpdates)
        {
            if (positionUpdates == null) return;

            foreach (var update in positionUpdates
                .Where(q => !string.IsNullOrWhiteSpace(q?.Aircraft))
                .OrderBy(q => q.TimeStamp)
                .ToList())
            {
                Enqueue(update);
            }
        }

        /// <summary>
        /// Retrieves all objects which are (roughly) nearby
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        // See https://stackoverflow.com/a/13579921/1720761 for more information about the clusterfuck that is coordinate notation
        public IEnumerable<FlightContext> FindNearby(Point coordinate, double distance = 0.2)
        {
            if (coordinate == null) throw new ArgumentException($"{nameof(coordinate)} should not be null");

            var nearbyPositions = _map.Nearby(new PositionUpdate(null, DateTime.MinValue, coordinate.Y, coordinate.X), distance);

            foreach (var position in nearbyPositions)
            {
                yield return _flightContextDictionary[position.Aircraft];
            }
        }

        /// <summary>
        /// The attach method can be used to add an already existing context instance to this factory.
        /// This method will overwrite any FlightContext instance with the same aircraft identifier already
        /// tracked by this FlightContextFactory.
        /// </summary>
        /// <param name="context"></param>
        public void Attach(FlightContext context) => Attach(context?.Flight.Metadata);

        /// <summary>
        /// This method creates a new FlightContext instance from the metadata and adds it to this factory.
        /// This method will overwrite any FlightContext instance with the same aircraft identifier already
        /// tracked by this FlightContextFactory.
        /// </summary>
        /// <param name="metadata"></param>
        public void Attach(FlightMetadata metadata)
        {
            if (metadata == null) return;

            _flightContextDictionary.TryRemove(metadata.Aircraft, out _);

            EnsureContextAvailable(metadata);
        }

        /// <summary>
        /// Retrieves the <seealso cref="FlightContext"/> from the factory. Please note that the <seealso cref="FlightContext"/> will still be attached to the factory.
        /// </summary>
        /// <param name="aircraft">The identifier of the aircraft for which you want to retrieve the context.</param>
        /// <returns></returns>
        public FlightContext GetContext(string aircraft)
        {
            if (_flightContextDictionary.TryGetValue(aircraft, out FlightContext context))
                return context;

            return null;
        }

        /// <summary>
        /// Removes the <seealso cref="FlightContext"/> for the specified aircraft, and returns the result.
        /// </summary>
        /// <param name="aircraft">The removed <seealso cref="FlightContext"/></param>
        /// <returns></returns>
        public FlightContext Detach(string aircraft)
        {
            if (_flightContextDictionary.TryRemove(aircraft, out FlightContext context))
                return context;

            return null;
        }

        /// <summary>
        /// Checks whether there is a context available for the aircraft which will be processed.
        /// </summary>
        /// <param name="aircraft"></param>
        private void EnsureContextAvailable(string aircraft)
        {
            EnsureContextAvailable(new FlightMetadata
            {
                Aircraft = aircraft
            });
        }

        /// <summary>
        /// Checks whether there's a context available based on the metadata provided.
        /// </summary>
        /// <param name="metadata"></param>
        private void EnsureContextAvailable(FlightMetadata metadata)
        {
            if (metadata?.Aircraft == null || _flightContextDictionary.ContainsKey(metadata.Aircraft)) return;

            var context = new FlightContext(metadata, options =>
            {
                options.AircraftId = metadata.Aircraft;
                options.MinifyMemoryPressure = Options.MinifyMemoryPressure;
                options.MinimumRequiredPositionUpdateCount = Options.MinimumRequiredPositionUpdateCount;
                options.NearbyRunwayAccessor = Options.NearbyRunwayAccessor;

                options.NearbyAircraftAccessor = ((Point location, double distance) search) =>
                {
                    return FindNearby(search.location, search.distance);
                };
            });
            SubscribeContextEventHandlers(context);

            _flightContextDictionary.TryAdd(metadata.Aircraft, context);
        }

        private void SubscribeContextEventHandlers(FlightContext context)
        {
            // Subscribe to the events so we can propagate 'em via the factory
            context.OnTakeoff += (sender, args) => OnTakeoff?.Invoke(sender, args);
            context.OnLaunchCompleted += (sender, args) => OnLaunchCompleted?.Invoke(sender, args);
            context.OnLanding += (sender, args) => OnLanding?.Invoke(sender, args);
            context.OnRadarContact += (sender, args) => OnRadarContact?.Invoke(sender, args);
            context.OnCompletedWithErrors += (sender, args) => OnCompletedWithErrors?.Invoke(sender, args);
        }

        /// <summary>
        /// The OnTakeoff event will fire once a takeoff is detected. Please note that the events from individual 
        /// FlightContext instances will be propagated through this event handler.
        /// </summary>
        public event EventHandler<OnTakeoffEventArgs> OnTakeoff;

        public IObservable<OnTakeoffEventArgs> Departures => Observable
            .FromEventPattern<OnTakeoffEventArgs>(
                (args) => OnTakeoff += args,
                (args) => OnTakeoff -= args)
            .Select(q => q.EventArgs);

        public event EventHandler<OnLaunchCompletedEventArgs> OnLaunchCompleted;

        public IObservable<OnLaunchCompletedEventArgs> CompletedLaunches => Observable
            .FromEventPattern<OnLaunchCompletedEventArgs>(
                (args) => OnLaunchCompleted += args,
                (args) => OnLaunchCompleted -= args)
            .Select(q => q.EventArgs);

        /// <summary>
        /// The OnLanding event will fire once a landing is detected. Please note that the events from individual 
        /// FlightContext instances will be propagated through this event handler.
        /// </summary>
        public event EventHandler<OnLandingEventArgs> OnLanding;

        public IObservable<OnLandingEventArgs> Arrivals => Observable
            .FromEventPattern<OnLandingEventArgs>(
                (args) => OnLanding += args,
                (args) => OnLanding -= args)
            .Select(q => q.EventArgs);

        /// <summary>
        /// The OnRadarContact event will fire when a takeoff has not been recorded but an aircraft is mid flight.
        /// Please note that the events from individual FlightContext instances will be propagated through this event
        /// handler.
        /// </summary>
        public event EventHandler<OnRadarContactEventArgs> OnRadarContact;

        public IObservable<OnRadarContactEventArgs> RadarContact => Observable
            .FromEventPattern<OnRadarContactEventArgs>(
                (args) => OnRadarContact += args,
                (args) => OnRadarContact -= args)
            .Select(q => q.EventArgs);

        /// <summary>
        /// The OnCompletedWithErrors event will fire when flight processing has been completed but some errors have 
        /// been detected (For example destination airfield could not be found). Please note that events from 
        /// individual FlightContext instances will be propagated through this event handler.
        /// </summary>
        public event EventHandler<OnCompletedWithErrorsEventArgs> OnCompletedWithErrors;

        public IObservable<OnCompletedWithErrorsEventArgs> Vanished => Observable
            .FromEventPattern<OnCompletedWithErrorsEventArgs>(
                (args) => OnCompletedWithErrors += args,
                (args) => OnCompletedWithErrors -= args)
            .Select(q => q.EventArgs);

        /// <summary>
        /// The OnContextDispose event will fire when a specific FlightContext instance is being disposed. Disposal of
        /// instances will happen if there is no activity for a specific time period.
        /// </summary>
        public event EventHandler<OnContextDisposedEventArgs> OnContextDispose;

        public IObservable<OnContextDisposedEventArgs> Untracked => Observable
            .FromEventPattern<OnContextDisposedEventArgs>(
                (args) => OnContextDispose += args,
                (args) => OnContextDispose -= args)
            .Select(q => q.EventArgs);
    }
}
