using Accord.MachineLearning;
using Accord.Statistics.Distributions.DensityKernels;
using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;

namespace Boerman.FlightAnalysis.Models
{
    [Serializable]
    public class FlightViewModel : FlightMetadata
    {
        public FlightViewModel() {
        }

        public FlightViewModel(Flight flight) {
            PositionUpdates = flight.PositionUpdates;
        }
        
        /// <summary>
        /// This function returns the in-flight hotspots. These are the locations where the aircraft has hung
        /// around for a longer than average period. For example thermalling. A mean-shift function is used to
        /// determine these hotspots.
        /// 
        /// Departure and arrival points are not included.
        /// </summary>
        /// <returns></returns>
        public IDictionary<Point, DateTime> Hotspots()
        {
            var algo = new MeanShift
            {
                Kernel = new UniformKernel(),
                // The second decimal position has an accuracy of up to 1.1 km.
                // It should be reasonable to expect one can thermal a bit in a 2x2km grid.
                Bandwidth = 0.002
            };

            // In the future we could add the climbrate to localize thermals, probably quite accurately.
            var data = PositionUpdates
                .Select(q => new double[] { q.Latitude, q.Longitude })
                .ToArray();

            var cluster = algo.Learn(data);
            var labels = cluster.Decide(data);

            var changes = new Dictionary<int, DateTime>();

            int previousLabel = 0;
            
            for (var i = 0; i < data.Length; i++)
            {
                var label = labels[i];
                if (previousLabel != label)
                {
                    // We have to filter the changes to reflect a decent amount of time in between the points. 10 seconds doesn't really cut it.
                    var time = PositionUpdates[i].TimeStamp;
                    if (changes.Any() && (time - changes.Last().Value) < TimeSpan.FromMinutes(2))
                        continue;

                    changes.Add(i, time);
                    previousLabel = label;
                }
            }
            

            // Use `cluster.Modes` to retrieve the groups that have been found.
            // Then we can check what parts of the flight are related to that cluster from and to what time.
            
            throw new NotImplementedException();
        }

        public List<PositionUpdate> PositionUpdates { get; set; }
    }
}
