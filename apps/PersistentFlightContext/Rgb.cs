using System;
using System.Linq;

namespace PersistentFlightContext
{
    internal struct Rgb : IEquatable<Rgb>
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public Hls ToHls()
        {
            var hls = new Hls();

            var data = new[] { R, G, B };

            var max = data.Max();
            var min = data.Min();

            var diff = max - min;


            hls.L = (max + min) / 2;
            if (Math.Abs(diff) < 0.00001)
            {
                hls.S = 0;
                hls.H = 0;  // H is really undefined.

                return hls;
            }

            if (hls.L <= 0.5) hls.S = diff / (max + min);
            else hls.S = diff / (2 - max - min);

            double r_dist = (max - R) / diff;
            double g_dist = (max - G) / diff;
            double b_dist = (max - B) / diff;

            hls.H =
                R == max
                    ? b_dist - g_dist
                    :
                G == max
                    ? 2 + r_dist - b_dist
                    : 4 + g_dist - r_dist;

            hls.H = hls.H * 60;

            if (hls.H < 0) hls.H += 360;

            return hls;
        }

        public override bool Equals(object obj)
        {
            var other = (Rgb)obj;

            return R == other.R
                && G == other.G
                && B == other.B;
        }

        public bool Equals(Rgb other)
        {
            return R == other.R &&
                     G == other.G &&
                     B == other.B;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B);
        }

        public static bool operator ==(Rgb left, Rgb right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rgb left, Rgb right)
        {
            return !(left == right);
        }
    }
}
