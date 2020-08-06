using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Priority_Queue;
using System.Reactive.Linq;
using Skyhop.FlightAnalysis.Models;
using Stateless;
using Stateless.Graph;
using Skyhop.FlightAnalysis.States;

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
            None,
            DetermineFlightState,
            DetermineLaunchMethod,
            TrackLaunchMethod,
            ProcessPoint,
            WaitingForData
        }

        public enum Trigger
        {
            Next,
            Standby,
            ResolveState,
            ResolveDeparture,
            ResolveLaunchMethod,
            CheckIfAerotow,
            CheckIfWinchLaunch,
            CheckIfSelfLaunch,
            TrackAerotow,
            TrackLaunch,
            ResolveArrival,
            Initialize
        }

        public readonly StateMachine<State, Trigger> StateMachine;

        public Flight Flight { get; internal set; }
        
        internal readonly FlightContextOptions Options = new FlightContextOptions();
        internal SimplePriorityQueue<PositionUpdate> PriorityQueue = new SimplePriorityQueue<PositionUpdate>();
        internal DateTime LatestTimeStamp;
        
        public DateTime LastActive { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public FlightContext(FlightContextOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlightContext"/> class.
        /// </summary>
        /// <param name="flightMetadata">When provided the flightMetadata parameter will set the flight information assuming previous 
        /// processing has been done.</param>
        public FlightContext(FlightMetadata flightMetadata, Action<FlightContextOptions> options)
        {
            StateMachine = new StateMachine<State, Trigger>(State.None, FiringMode.Queued);

            StateMachine.Configure(State.None)
                .Permit(Trigger.Next, State.ProcessPoint);

            StateMachine.Configure(State.ProcessPoint)
                .OnEntry(this.ProcessNextPoint)
                .PermitReentry(Trigger.Next)
                .InternalTransition(Trigger.Initialize, this.Initialize)
                .Permit(Trigger.Standby, State.WaitingForData)
                .Permit(Trigger.ResolveState, State.DetermineFlightState);

            StateMachine.Configure(State.DetermineFlightState)
                .OnEntry(this.DetermineFlightState)
                .PermitReentry(Trigger.ResolveState)
                .InternalTransition(Trigger.ResolveDeparture, this.FindDepartureHeading)
                .InternalTransition(Trigger.ResolveArrival, this.FindArrivalHeading)
                .Permit(Trigger.Next, State.ProcessPoint)
                .Permit(Trigger.ResolveLaunchMethod, State.DetermineLaunchMethod);

            StateMachine.Configure(State.DetermineLaunchMethod)
                .OnEntry(this.DetermineLaunchMethod)
                .PermitReentry(Trigger.ResolveLaunchMethod)
                .InternalTransition(Trigger.CheckIfAerotow, this.IsAerotow)
                .InternalTransition(Trigger.CheckIfWinchLaunch, this.IsWinchLaunch)
                .InternalTransition(Trigger.CheckIfSelfLaunch, this.IsSelfLaunch)
                .Permit(Trigger.Next, State.ProcessPoint);

            StateMachine.Configure(State.TrackLaunchMethod)
                .OnEntry(this.TrackLaunch)
                .PermitReentry(Trigger.TrackLaunch);

            StateMachine.Configure(State.WaitingForData)
                .PermitReentry(Trigger.Standby)
                .Permit(Trigger.Next, State.ProcessPoint);

            options?.Invoke(Options);

            Options.AircraftId = flightMetadata.Aircraft;   // This line prevents the factory from crashing when the attach method is used.
            Flight = flightMetadata.Flight;

            LastActive = DateTime.UtcNow;
        }

        /// <summary>
        /// FlightContext Constructor
        /// </summary>
        /// <param name="aircraftId">Optional string used to identify this context.</param>
        public FlightContext(string aircraftId, Action<FlightContextOptions> options = default) : this(
            new FlightMetadata
            {
                Aircraft = aircraftId
            },
            options) { }

        public FlightContext(FlightMetadata flightMetadata) : this(flightMetadata, default) { }

        /// <summary>
        /// Queue a positionupdate for this specific context to process.
        /// </summary>
        /// <param name="positionUpdate">The positionupdate to queue</param>
        /// <param name="startOrContinueProcessing">Whether or not to start/continue processing</param>
        public void Enqueue(PositionUpdate positionUpdate)
        {
            if (positionUpdate == null) return;

            PriorityQueue.Enqueue(positionUpdate, positionUpdate.TimeStamp.Ticks);

            if (StateMachine.IsInState(State.WaitingForData) || StateMachine.IsInState(State.None))
            {
                StateMachine.Fire(Trigger.Next);
            }

            LastActive = DateTime.UtcNow;
        }

        /// <summary>
        /// Queue a collection of position updates on this context for processing. Processing will either start 
        /// directly or continue in case it is still running.
        /// </summary>
        /// <param name="positionUpdates">The position updates to queue</param>
        public void Enqueue(IEnumerable<PositionUpdate> positionUpdates)
        {
            if (!positionUpdates.Any()) return;

            foreach (var update in positionUpdates)
            {
                PriorityQueue.Enqueue(update, update.TimeStamp.Ticks);
            }

            if (StateMachine.IsInState(State.WaitingForData) || StateMachine.IsInState(State.None))
            {
                StateMachine.Fire(Trigger.Next);
            }

            LastActive = DateTime.UtcNow;
        }

        /// <summary>
        /// This method casually removes some position updates.
        /// </summary>
        internal void CleanupDataPoints()
        {
            if (Options.MinifyMemoryPressure && Flight.PositionUpdates.Count > Options.MinimumRequiredPositionUpdateCount)
                Flight.PositionUpdates.RemoveRange(0, Flight.PositionUpdates.Count - Options.MinimumRequiredPositionUpdateCount);
        }

        public string ToDotGraph()
        {
            return UmlDotGraph.Format(StateMachine.GetInfo());
        }

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
            }
        }

        internal void InvokeOnLaunchCompletedEvent()
        {
            try
            {
                OnLaunchCompleted?.Invoke(this, new OnLaunchCompletedEventArgs(Flight));
            } catch (Exception ex)
            {
                Trace.Write(ex);
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
