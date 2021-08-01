using NetTopologySuite.Geometries;
using Skyhop.FlightAnalysis.Models;
using Skyhop.SpatialMap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

namespace Skyhop.FlightAnalysis
{
    /// <summary>
    /// The FlightContextFactory is a singleton instance capable of handling data inputs from multiple aircraft and 
    /// manages multiple FlightContext instances based on the provided data.
    /// </summary>
    public class FlightContextFactory
    {
        // ToDo: Possible performance improvement over here
        private readonly SpatialMap<PositionUpdate> _map = new SpatialMap<PositionUpdate>(
            q => q != null ? Math.Cos(Math.PI / 180 * q.Location.Y) * 111 * q.Location.X : 0,
            q => q != null ? q.Location.Y * 111 : 0);

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
        }

        public FlightContextFactory(IEnumerable<Flight> flights, Action<FlightContextFactoryOptions> options = default)
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            if (flights == null) throw new ArgumentException("Argument cannot be null", nameof(flights));
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            foreach (var flight in flights)
            {
                _ensureContextAvailable(flight);
            }

            options?.Invoke(Options);
        }

        public IEnumerable<string> TrackedAircraft => _flightContextDictionary.Select(q => q.Key);

        /// <summary>
        /// Queue a position update on the FlightContextFactory. The FlightContextFactory will handle further 
        /// processing of this position update.
        /// </summary>
        /// <param name="positionUpdate">The position update to queue</param>
        public void Process(PositionUpdate positionUpdate)
        {
            if (positionUpdate == null) return;

            _ensureContextAvailable(positionUpdate.Aircraft);

            if (_flightContextDictionary.TryGetValue(positionUpdate.Aircraft, out var flightContext))
            {
                var previousPoint = flightContext.CurrentPosition;

                if (flightContext.Process(positionUpdate))
                {
                    _map.Add(positionUpdate);
                    _map.Remove(previousPoint);

                    if (_map.Nearby(previousPoint, 2).ToList().Count(q => q.Aircraft == positionUpdate.Aircraft) > 1)
                    {
                        Debugger.Break();
                    }
                }
            }
        }

        /// <summary>
        /// Queue a collection of position updates on the FlightContextFactory. The FlightContextFactory will handle
        /// further processing of these position updates.
        /// </summary>
        /// <param name="positionUpdates">The position updates to queue</param>
        public void Process(IEnumerable<PositionUpdate> positionUpdates)
        {
            if (positionUpdates == null) return;

            foreach (var update in positionUpdates
                .Where(q => !string.IsNullOrWhiteSpace(q?.Aircraft))
                .OrderBy(q => q.TimeStamp)
                .ToList())
            {
                Process(update);
            }
        }

        /// <summary>
        /// Retrieves all objects which are (roughly) nearby
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="distance">Distance in kilometers</param>
        /// 
        /// <returns></returns>
        // See https://stackoverflow.com/a/13579921/1720761 for more information about the clusterfuck that is coordinate notation
        public IEnumerable<FlightContext> FindNearby(Point coordinate, double distance = 0.2)
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            if (coordinate == null) throw new ArgumentException($"{nameof(coordinate)} should not be null");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            var nearbyPositions = _map.Nearby(new PositionUpdate(null, DateTime.MinValue, coordinate.Y, coordinate.X), distance);

            foreach (var position in nearbyPositions)
            {
                yield return _flightContextDictionary[position.Aircraft];
            }
        }

        public IEnumerable<FlightContext> FindNearby(FlightContext context, double distance = 0.2)
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            if (context == null) throw new ArgumentException($"{nameof(context)} should not be null");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

            var nearbyPositions = _map.Nearby(new PositionUpdate(null, DateTime.MinValue, context.CurrentPosition.Location.Y, context.CurrentPosition.Location.X), distance);

            foreach (var position in nearbyPositions)
            {
                if (position.Aircraft != context.Options.AircraftId)
                {
                    yield return _flightContextDictionary[position.Aircraft];
                }
            }
        }

        /// <summary>
        /// The attach method can be used to add an already existing context instance to this factory.
        /// This method will overwrite any FlightContext instance with the same aircraft identifier already
        /// tracked by this FlightContextFactory.
        /// </summary>
        /// <param name="context"></param>
        public void Attach(FlightContext context) => Attach(context?.Flight);

        /// <summary>
        /// This method creates a new FlightContext instance from the metadata and adds it to this factory.
        /// This method will overwrite any FlightContext instance with the same aircraft identifier already
        /// tracked by this FlightContextFactory.
        /// </summary>
        /// <param name="flight"></param>
        public void Attach(Flight flight)
        {
            if (flight == null) return;

            _flightContextDictionary.TryRemove(flight.Aircraft, out _);

            _ensureContextAvailable(flight);
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
        private void _ensureContextAvailable(string aircraft)
        {
            _ensureContextAvailable(new Flight
            {
                Aircraft = aircraft
            });
        }

        /// <summary>
        /// Checks whether there's a context available based on the metadata provided.
        /// </summary>
        /// <param name="flight"></param>
        private void _ensureContextAvailable(Flight flight)
        {
            if (flight?.Aircraft == null || _flightContextDictionary.ContainsKey(flight.Aircraft)) return;

            var context = new FlightContext(flight, options =>
            {
                options.AircraftId = flight.Aircraft;
                options.NearbyRunwayAccessor = Options.NearbyRunwayAccessor;

                options.AircraftAccessor = 
                    aircraft => _flightContextDictionary.TryGetValue(aircraft, out var flightContext)
                        ? flightContext
                        : null;

                options.NearbyAircraftAccessor = (Point location, double distance) =>
                {
                    return FindNearby(location, distance)
                        .Where(q => q.Options.AircraftId != flight.Aircraft)
                        .ToList();
                };
            });
            _subscribeContextEventHandlers(context);

            _flightContextDictionary.TryAdd(flight.Aircraft, context);
        }

        private void _subscribeContextEventHandlers(FlightContext context)
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
        #pragma warning disable CS0067
        public event EventHandler<OnContextDisposedEventArgs> OnContextDispose;
        #pragma warning restore CS0067

        public IObservable<OnContextDisposedEventArgs> Untracked => Observable
            .FromEventPattern<OnContextDisposedEventArgs>(
                (args) => OnContextDispose += args,
                (args) => OnContextDispose -= args)
            .Select(q => q.EventArgs);
    }
}
