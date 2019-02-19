using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Boerman.FlightAnalysis.Models
{
    public struct Rgb
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public Hls ToHls()
        {
            var hls = new Hls();

            // Get the maximum and minimum RGB components.
            var data = new[] { R, G, B };

            var max = data.Max();
            var min = data.Min();

            var diff = max - min;

            var result = new Hls();

            result.L = (max + min) / 2;
            if (Math.Abs(diff) < 0.00001)
            {
                result.S = 0;
                result.H = 0;  // H is really undefined.

                return result;
            }
            
            if (result.L <= 0.5) result.S = diff / (max + min);
            else result.S = diff / (2 - max - min);

            double r_dist = (max - R) / diff;
            double g_dist = (max - G) / diff;
            double b_dist = (max - B) / diff;

            result.H =
                (R == max)
                    ? b_dist - g_dist
                    :
                (G == max)
                    ? 2 + r_dist - b_dist
                    : 4 + g_dist - r_dist;

            result.H = result.H * 60;
            if (result.H < 0) result.H += 360;
            throw new NotImplementedException();
        }
    }
}
