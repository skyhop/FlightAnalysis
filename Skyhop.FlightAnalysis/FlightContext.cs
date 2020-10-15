using Skyhop.FlightAnalysis.Models;
using Stateless;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

namespace Skyhop.FlightAnalysis
{
    /// <summary>
    /// The FlightContext is an aircraft specific instance which analyses the flight's position updates.
    /// 
    /// To process data points from multiple aircraft use the FlightContextFactory.
    /// </summary>
    public class FlightContext
    {
        public enum State
        {
            None = 0,
            Initialization = 1,
            Stationary = 2,
            Departing = 3,
            TrackAerotow = 4,
            Airborne = 5,
            Arriving = 6
        }

        internal enum Trigger
        {
            Initialize,
            Next,
            TrackMovements,
            Depart,
            TrackAerotow,
            LaunchCompleted,
            Landing,
            LandingAborted,
            Arrived
        }

        internal readonly StateMachine<State, Trigger> StateMachine;

        public Flight Flight { get; internal set; }

        internal readonly FlightContextOptions Options = new FlightContextOptions();
        internal PositionUpdate CurrentPosition;
        internal DateTime LatestTimeStamp;

        public FlightContext(FlightContextOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlightContext"/> class.
        /// </summary>
        /// <param name="flight">When provided the flightMetadata parameter will set the flight information assuming previous 
        /// processing has been done.</param>
        public FlightContext(Flight flight, Action<FlightContextOptions> options)
        {
            StateMachine = new StateMachine<State, Trigger>(
                () => Flight.State,
                state => Flight.State = state,
                FiringMode.Queued);

            StateMachine.Configure(State.None)
                .Permit(Trigger.Next, State.Initialization);

            StateMachine.Configure(State.Initialization)
                .OnEntry(this.Initialize)
                .Permit(Trigger.Next, State.Stationary);

            StateMachine.Configure(State.Stationary)
                .OnEntry(this.Stationary)
                .PermitReentry(Trigger.Next)
                .Permit(Trigger.TrackMovements, State.Airborne)
                .Permit(Trigger.Depart, State.Departing);

            StateMachine.Configure(State.Departing)
                .OnEntry(this.Departing)
                .PermitReentry(Trigger.Next)
                .Permit(Trigger.LaunchCompleted, State.Airborne)
                .Permit(Trigger.Landing, State.Arriving)
                .Permit(Trigger.TrackAerotow, State.TrackAerotow);

            StateMachine.Configure(State.TrackAerotow)
                .OnEntry(this.TrackAerotow)
                .PermitReentry(Trigger.Next)
                .Permit(Trigger.LaunchCompleted, State.Airborne);

            StateMachine.Configure(State.Airborne)
                .OnEntry(this.Airborne)
                .PermitReentry(Trigger.Next)
                .Permit(Trigger.Landing, State.Arriving);

            StateMachine.Configure(State.Arriving)
                .OnEntry(this.Arriving)
                .PermitReentry(Trigger.Next)
                .Permit(Trigger.Arrived, State.Initialization)
                .Permit(Trigger.LandingAborted, State.Airborne);

            options?.Invoke(Options);

            Options.AircraftId = flight.Aircraft;   // This line prevents the factory from crashing when the attach method is used.
            Flight = flight;
        }

        /// <summary>
        /// FlightContext Constructor
        /// </summary>
        /// <param name="aircraftId">Optional string used to identify this context.</param>
        public FlightContext(string aircraftId, Action<FlightContextOptions> options = default) : this(
            new Flight
            {
                Aircraft = aircraftId
            },
            options)
        { }

        public FlightContext(Flight flight) : this(flight, default) { }

        /// <summary>
        /// Queue a positionupdate for this specific context to process.
        /// </summary>
        /// <param name="positionUpdate">The positionupdate to queue</param>
        /// <param name="startOrContinueProcessing">Whether or not to start/continue processing</param>
        public bool Process(PositionUpdate positionUpdate)
        {
            if (positionUpdate == null) return false;

            if (CurrentPosition != null)
            {
                if ((positionUpdate.TimeStamp - CurrentPosition.TimeStamp).TotalMilliseconds < 100) return false;
                if (positionUpdate.Speed > 0 &&
                    CurrentPosition.Longitude == positionUpdate.Longitude
                    && CurrentPosition.Latitude == positionUpdate.Latitude) return false;
            }

            CurrentPosition = positionUpdate;

            StateMachine.Fire(Trigger.Next);

            if (Flight.DepartureTime == null &&
                CurrentPosition?.Speed == 0)
            {
                // We generally do not need to analyse ground based movements, hence discard this point.
                Flight.PositionUpdates.Clear();
            }

            Flight.PositionUpdates.Add(positionUpdate);

            return true;
        }

        /// <summary>
        /// Queue a collection of position updates on this context for processing. Processing will either start 
        /// directly or continue in case it is still running.
        /// </summary>
        /// <param name="positionUpdates">The position updates to queue</param>
        public void Process(IEnumerable<PositionUpdate> positionUpdates)
        {
            foreach (var update in positionUpdates
                .OrderBy(q => q.TimeStamp)
                .ToList())
            {
                Process(update);
            }
        }

        public PositionUpdate GetPositionAt(DateTime timestamp)
        {
            if (Flight.PositionUpdates.Count < 2) return null;

            var first = Flight.PositionUpdates[0].TimeStamp;
            var last = Flight.PositionUpdates[Flight.PositionUpdates.Count - 1].TimeStamp;

            PositionUpdate p1 = null;
            PositionUpdate p2 = null;

            if (timestamp <= first)
            {
                p1 = Flight.PositionUpdates[0];
                p2 = Flight.PositionUpdates[1];
            } else if (timestamp >= last)
            {
                p1 = Flight.PositionUpdates[Flight.PositionUpdates.Count - 2];
                p2 = Flight.PositionUpdates[Flight.PositionUpdates.Count - 1];
            } else
            {
                var index = Flight.PositionUpdates.FindIndex(q => q.TimeStamp >= timestamp);
                p1 = Flight.PositionUpdates[index - 1];
                p2 = Flight.PositionUpdates[index];
            }

            if (p1.TimeStamp.Equals(timestamp)) return p1;
            else if (p2.TimeStamp.Equals(timestamp)) return p2;

            var dX = p2.Location.X - p1.Location.X;
            var dY = p2.Location.Y - p1.Location.Y;
            var dT = (p2.TimeStamp - p1.TimeStamp).Ticks;
            var dA = p2.Altitude - p1.Altitude;
            var dS = p2.Speed - p1.Speed;

            if (dT == 0) return null;

            double factor = (timestamp.Ticks - p1.TimeStamp.Ticks) / (double)dT;

            return new PositionUpdate(
                Options.AircraftId,
                new DateTime((long)(p1.TimeStamp.Ticks + dT * factor)),
                p1.Location.Y + factor * dY,
                p1.Location.X + factor * dX,
                p1.Altitude + dA * factor,
                p1.Speed + dS * factor,
                double.NaN);
        }

#if DEBUG
        public string ToDotGraph()
        {
            return UmlDotGraph.Format(StateMachine.GetInfo());
        }
#endif

        /*
         * By wrapping the event invocation in try catch blocks we can prevent that the context abruptly ends because
         * of an exception through the event handler.
         */
        internal void InvokeOnTakeoffEvent()
        {
            try
            {
                OnTakeoff?.Invoke(this, new OnTakeoffEventArgs(Flight));
            }
            catch (Exception ex)
            {
                Trace.Write(ex);
                throw;
            }
        }

        internal void InvokeOnLaunchCompletedEvent()
        {
            try
            {
                OnLaunchCompleted?.Invoke(this, new OnLaunchCompletedEventArgs(Flight));
            }
            catch (Exception ex)
            {
                Trace.Write(ex);
                throw;
            }
        }

        internal void InvokeOnLandingEvent()
        {
            try
            {
                OnLanding?.Invoke(this, new OnLandingEventArgs(Flight));
            }
            catch (Exception ex)
            {
                Trace.Write(ex);
                throw;
            }
        }

        internal void InvokeOnRadarContactEvent()
        {
            try
            {
                OnRadarContact?.Invoke(this, new OnRadarContactEventArgs(Flight));
            }
            catch (Exception ex)
            {
                Trace.Write(ex);
                throw;
            }
        }

        internal void InvokeOnCompletedWithErrorsEvent()
        {
            try
            {
                OnCompletedWithErrors?.Invoke(this, new OnCompletedWithErrorsEventArgs(Flight));
            }
            catch (Exception ex)
            {
                Trace.Write(ex);
                throw;
            }
        }

        /// <summary>
        /// The OnTakeoff event will fire once the data indicates a takeoff.
        /// </summary>
        public event EventHandler<OnTakeoffEventArgs> OnTakeoff;

        /// <summary>
        /// Observable source for live updates on departures.
        /// </summary>
        public IObservable<OnTakeoffEventArgs> Departure => Observable
            .FromEventPattern<OnTakeoffEventArgs>(
                (args) => OnTakeoff += args,
                (args) => OnTakeoff -= args)
            .Select(q => q.EventArgs);

        public event EventHandler<OnLaunchCompletedEventArgs> OnLaunchCompleted;

        public IObservable<OnLaunchCompletedEventArgs> LaunchCompleted => Observable
            .FromEventPattern<OnLaunchCompletedEventArgs>(
                (args) => OnLaunchCompleted += args,
                (args) => OnLaunchCompleted -= args)
            .Select(q => q.EventArgs);

        /// <summary>
        /// The OnLanding event will fire once the data indicates a landing.
        /// </summary>
        public event EventHandler<OnLandingEventArgs> OnLanding;

        /// <summary>
        /// Observable source for live updates on arrivals.
        /// </summary>
        public IObservable<OnLandingEventArgs> Arrival => Observable
            .FromEventPattern<OnLandingEventArgs>(
                (args) => OnLanding += args,
                (args) => OnLanding -= args)
            .Select(q => q.EventArgs);

        /// <summary>
        /// The OnRadarContact event will fire when a takeoff has not been recorded but an aircraft is mid flight
        /// </summary>
        public event EventHandler<OnRadarContactEventArgs> OnRadarContact;

        /// <summary>
        /// Observable source to get notified when a new aircraft is being tracked.
        /// </summary>
        public IObservable<OnRadarContactEventArgs> RadarContact => Observable
            .FromEventPattern<OnRadarContactEventArgs>(
                (args) => OnRadarContact += args,
                (args) => OnRadarContact -= args)
            .Select(q => q.EventArgs);

        /// <summary>
        /// The OnCompletedWithErrors event will fire when flight processing has been completed but some errors have 
        /// been detected. (For example destination airfield could not be found)
        /// </summary>
        public event EventHandler<OnCompletedWithErrorsEventArgs> OnCompletedWithErrors;

        /// <summary>
        /// Observable source to get notified when a certain tracked aircraft hasn't been heard of in a while, 
        /// while it hasn't been observed to have landed.
        /// </summary>
        public IObservable<OnCompletedWithErrorsEventArgs> Vanished => Observable
            .FromEventPattern<OnCompletedWithErrorsEventArgs>(
                (args) => OnCompletedWithErrors += args,
                (args) => OnCompletedWithErrors -= args)
            .Select(q => q.EventArgs);
    }
}
