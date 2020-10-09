using System;

namespace BulkFlightDataProcessing
{
    public class CsvData
    {
        public string Aircraft { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Heading { get; set; }
        public DateTime Timestamp { get; set; }
        public double Altitude { get; set; }
        public short Speed { get; set; }
    }
}
