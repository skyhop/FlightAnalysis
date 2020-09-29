using Skyhop.FlightAnalysis.Experimental;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var flightContext = new FlightContext("");

            Console.Write(flightContext.ToDotGraph());

            TextCopy.ClipboardService.SetText(flightContext.ToDotGraph());

            Console.ReadKey();
        }
    }
}
