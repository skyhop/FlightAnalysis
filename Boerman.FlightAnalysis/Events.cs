using System;
namespace Boerman.FlightAnalysis
{
    public static class Events
    {
        public static void UnsubscribeSubscribers(EventHandler eventHandler) {
            if (eventHandler == null) return;

            var delegates = eventHandler.GetInvocationList();

            foreach (var d in delegates) {
                eventHandler -= (d as EventHandler);
            }
        }
    }
}
