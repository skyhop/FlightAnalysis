using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Boerman.FlightAnalysis.FlightStates;
using Boerman.FlightAnalysis.Models;
using Boerman.Core.Extensions;
using Boerman.Core.State;
using Priority_Queue;
using System.Reactive.Linq;

namespace Boerman.FlightAnalysis
{
    /// <summary>
    /// The FlightContext is an aircraft specific instance which analyses the flight's position updates.
    /// 
    /// To process data points from multiple aircraft use the FlightContextFactory.
    /// </summary>
    public class FlightContext : BaseContext
    {
        internal const int MinimumRequiredPositionUpdateCount = 5;
        internal bool MinifyMemoryPressure;

        internal readonly string AircraftId;
        public Flight Flight { get; internal set; }

        internal SimplePriorityQueue<PositionUpdate> PriorityQueue = new SimplePriorityQueue<PositionUpdate>();

        internal DateTime LatestTimeStamp;

        public CancellationTokenSource CancellationTokenSource { get; private set; }

        /*
         * This is the only constructor which is allowed to initiale a FlightContext without aircraft identifier.
         * Only to facilitate the few moments where your workflow is set up in a way that it's logical to only process
         * data from a single aircraft. For all other reasons I'd strongly recommend using the FlightContextFactory
         * anyway.
         */
        
        /// <summary>
        /// FlightContext Constructor
        /// </summary>
        /// <param name="aircraftId">Optional string used to identify this context.</param>
        public FlightContext(string aircraftId, bool minifyMemoryPressure)
        {
            AircraftId = aircraftId;

            MinifyMemoryPressure = minifyMemoryPressure;

            Flight = new Flight
            {
                Aircraft = aircraftId
            };
            
            QueueState(typeof(InitializeFlightState));

            /*
             * While one might think that process execution is being started from the constructor this is not the case.
             * The `StartOrContinueProcessing` call continues to call the `Run` function, which exists in the `BaseContext`
             * class. The `Run` function in turn queues the state and continues execution of the state onto the threadpool.
             * Therefore only initial state scheduling happens during construction of the instance.
             * 
             * For more information go check out the source code at https://github.com/Boerman/Boerman.Core/tree/master/State
             */
            StartOrContinueProcessing();
        }

        public FlightContext(string aircraftId) : this(aircraftId, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlightContext"/> class.
        /// </summary>
        /// <param name="flightMetadata">When provided the flightMetadata parameter will set the flight information assuming previous 
        /// processing has been done.</param>
        public FlightContext(FlightMetadata flightMetadata, bool minifyMemoryPressure) {
            MinifyMemoryPressure = minifyMemoryPressure;

            AircraftId = flightMetadata.Aircraft;   // This line prevents the factory from crashing when the attach method is used.
            Flight = flightMetadata.Flight;
        }

        public FlightContext(FlightMetadata flightMetadata) : this(flightMetadata, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlightContext"/> class.
        /// </summary>
        /// <param name="flight">When provided the flight parameter will set the flight information assuming previous 
        /// processing has been done.</param>
        public FlightContext(Flight flight, bool minifyMemoryPressure) {
            if(String.IsNullOrWhiteSpace(flight.Aircraft)) throw new ArgumentException($"{nameof(flight.Aircraft)} cannot be null or empty");

            AircraftId = flight.Aircraft;
            Flight = flight;

            StartOrContinueProcessing();
        }

        public FlightContext(Flight flight) : this(flight, false) { }

        /// <summary>
        /// Queue a positionupdate for this specific context to process.
        /// </summary>
        /// <param name="positionUpdate">The positionupdate to queue</param>
        /// <param name="startOrContinueProcessing">Whether or not to start/continue processing</param>
        void Enqueue(PositionUpdate positionUpdate, bool startOrContinueProcessing)
        {
            if (positionUpdate == null) return;

            if (positionUpdate.TimeStamp > Flight.LastSeen) Flight.LastSeen = positionUpdate.TimeStamp;
            
            PriorityQueue.Enqueue(positionUpdate, positionUpdate.TimeStamp.Ticks);

            if (startOrContinueProcessing) StartOrContinueProcessing();
        }

        /// <summary>
        /// Queue a position update on this context for processing. Processing will either start directly or continue 
        /// in case it is still running.
        /// </summary>
        /// <param name="positionUpdate">The position update to queue</param>
        public void Enqueue(PositionUpdate positionUpdate)
        {
            Enqueue(positionUpdate, true);
        }
        
        /// <summary>
        /// Queue a collection of position updates on this context for processing. Processing will either start 
        /// directly or continue in case it is still running.
        /// </summary>
        /// <param name="positionUpdates">The position updates to queue</param>
        public void Enqueue(IEnumerable<PositionUpdate> positionUpdates)
        {
            positionUpdates.AsParallel().ForEach(q => Enqueue(q, false));
            StartOrContinueProcessing();
        }

        /// <summary>
        /// Starts or continues processing the queued data in this instance
        /// </summary>
        private void StartOrContinueProcessing()
        {
            if (IsQueueRunning) return;
            if (!StateQueueContainsStates) QueueState(typeof(ProcessNextPoint));

            CancellationTokenSource = new CancellationTokenSource();
            Run(CancellationTokenSource.Token);
        }

        /// <summary>
        /// This method casually removes some position updates.
        /// </summary>
        internal void CleanupDataPoints()
        {
            if (Flight.PositionUpdates.Count > MinimumRequiredPositionUpdateCount)
                Flight.PositionUpdates.RemoveRange(0, Flight.PositionUpdates.Count - MinimumRequiredPositionUpdateCount);
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

        private new bool StateQueueContainsStates => base.StateQueueContainsStates;
        public new Func<bool> WaitForIdleProcess => base.WaitForIdleProcess;
        public new bool IsQueueRunning => base.IsQueueRunning;

        protected new internal BaseContext QueueState(Type state)
        {
            base.QueueState(state);
            return this;
        }

        protected new internal void Run(CancellationToken cancellationToken = default)
        {
            base.Run(cancellationToken);
        }
    }
}
