using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Skyhop.FlightAnalysis.Models;
using Stateless;
using Stateless.Graph;

namespace Skyhop.FlightAnalysis.Experimental
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
            Initialization,
            Stationary,
            Departing,
            Airborne,
            Arriving
        }

        public enum Trigger
        {
            Initialize,
            Next,
            TrackMovements,
            Depart,
            LaunchCompleted,
            Landing,
            LandingAborted,
            Arrived
        }

        internal CancellationTokenSource ArrivalTheory = null;

        public readonly StateMachine<State, Trigger> StateMachine;

        public Flight Flight { get; internal set; }

        internal readonly FlightContextOptions Options = new FlightContextOptions();
        internal PositionUpdate CurrentPosition;
        internal DateTime LatestTimeStamp;

        public DateTime LastActive { get; private set; }

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
                .Permit(Trigger.Landing, State.Arriving);

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
            options)
        { }

        public FlightContext(FlightMetadata flightMetadata) : this(flightMetadata, default) { }

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

            /*
             * In order to prevent issues inserting the flight data into a spatial map we'll add a degree of randomness 
             * to the reported position updates to prevent this from becoming a big issue.
             * 
             * Given a double supports about 16 digits, and there are about 5 required to represent the distance from the mean line
             * We are left with 11 digits after the decimal.
             * 
             * 0    1               -   kilometer
             * 1    0.1             -   100 meter
             * 2    0.01            -   10 meter
             * 3    0.001           -   1 meter
             * 4    0.0001          -   10 centimeter
             * 5    0.00001         -   1 centimeter
             * 6    0.000001        -   1 mm
             * 7    0.0000001       -   0.1 mm
             * 8    0.00000001      -   0.01 mm
             * 9    0.000000001     -   0.001 mm
             * 10   0.0000000001    -   0.0001 mm
             * 11   0.00000000001   -   0.00001 mm
             * 
             * As an accuracy of 10 centimeters is close enough for our application, we still have 7 digits left.
             */


            CurrentPosition = positionUpdate;

            StateMachine.Fire(Trigger.Next);

            if (Flight.StartTime == null && 
                CurrentPosition?.Speed == 0)
            {
                // We generally do not need to analyse ground based movements, hence discard this point.
                Flight.PositionUpdates.Clear();
            }

            Flight.PositionUpdates.Add(positionUpdate);
            
            LastActive = DateTime.UtcNow;

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

#warning REMOVE BEFORE PUBLISH
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
