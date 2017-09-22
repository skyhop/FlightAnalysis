using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Boerman.Aeronautics.FlightAnalysis.FlightStates;
using Boerman.Aeronautics.FlightAnalysis.Models;
using Boerman.Core.Extensions;
using Boerman.Core.State;
using C5;

namespace Boerman.Aeronautics.FlightAnalysis
{
    /// <summary>
    /// The FlightContext is an aircraft specific instance which analyses the flight's position updates.
    /// 
    /// To process data points from multiple aircraft use the FlightContextFactory.
    /// </summary>
    public class FlightContext : BaseContext
    {
        internal readonly string AircraftId;
        public Flight Flight;
        //internal System.Collections.Generic.ICollection<PositionUpdate> PositionUpdates;

        internal IntervalHeap<long> Heap = new IntervalHeap<long>();
        internal ConcurrentDictionary<long, PositionUpdate> Data = new ConcurrentDictionary<long, PositionUpdate>();

        internal DateTime LatestTimeStamp;

        public CancellationTokenSource CancellationTokenSource;
        
        /// <summary>
        /// FlightContext Constructor
        /// </summary>
        /// <param name="aircraftId">Optional string used to identify this context.</param>
        public FlightContext(string aircraftId = null)
        {
            AircraftId = aircraftId;

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

        /// <summary>
        /// Queue a positionupdate for this specific context to process.
        /// </summary>
        /// <param name="positionUpdate">The positionupdate to queue</param>
        /// <param name="startOrContinueProcessing">Whether or not to start/continue processing</param>
        private void Enqueue(PositionUpdate positionUpdate, bool startOrContinueProcessing)
        {
            Heap.Add(positionUpdate.TimeStamp.Ticks);
            Data.TryAdd(positionUpdate.TimeStamp.Ticks, positionUpdate);

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

        /*
         * By wrapping the event invocation in try catch blocks we can prevent that the context abruptly ends because
         * of an exception through the event handler.
         */

        internal void InvokeOnTakeoffEvent()
        {
            try
            {
                OnTakeoff?.Invoke(this, new OnTakeoffEventArgs(Flight));
            } catch { }
        }
        
        internal void InvokeOnLandingEvent()
        {
            try
            {
                OnLanding?.Invoke(this, new OnLandingEventArgs(Flight));
            } catch { }
        }

        internal void InvokeOnCompletedWithErrorsEvent()
        {
            try
            {
                OnCompletedWithErrors?.Invoke(this, new OnCompletedWithErrorsEventArgs(Flight));
            } catch { }
        }

        /// <summary>
        /// The OnTakeoff event will fire once the data indicates a takeoff.
        /// </summary>
        public event EventHandler<OnTakeoffEventArgs> OnTakeoff;

        /// <summary>
        /// The OnLanding event will fire once the data indicates a landing
        /// </summary>
        public event EventHandler<OnLandingEventArgs> OnLanding;

        /// <summary>
        /// The OnCompletedWithErrors event will fire when flight processing has been completed but some errors have 
        /// been detected. (For example destination airfield could not be found)
        /// </summary>
        public event EventHandler<OnCompletedWithErrorsEventArgs> OnCompletedWithErrors;
    }
}
