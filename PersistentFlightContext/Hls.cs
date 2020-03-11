namespace PersistentFlightContext
{
    internal struct Hls
    {
        public double H { get; set; }
        public double L { get; set; }
        public double S { get; set; }

        public Rgb ToRgb()
        {
            double p2;
            if (L <= 0.5) p2 = L * (1 + S);
            else p2 = L + S - L * S;

            double p1 = 2 * L - p2;
            double double_r, double_g, double_b;
            if (S == 0)
            {
                double_r = L;
                double_g = L;
                double_b = L;
            }
            else
            {
                double qqhToRgb(double q1, double q2, double hue)
                {
                    if (hue > 360) hue -= 360;
                    else if (hue < 0) hue += 360;

                    if (hue < 60) return q1 + (q2 - q1) * hue / 60;
                    if (hue < 180) return q2;
                    if (hue < 240) return q1 + (q2 - q1) * (240 - hue) / 60;
                    return q1;
                }

                double_r = qqhToRgb(p1, p2, H + 120);
                double_g = qqhToRgb(p1, p2, H);
                double_b = qqhToRgb(p1, p2, H - 120);
            }

            // Convert RGB to the 0 to 255 range.
            return new Rgb
            {
                R = (byte)(double_r * 255.0),
                G = (byte)(double_g * 255.0),
                B = (byte)(double_b * 255.0)
            };
        }

        public override bool Equals(object obj)
        {
            var other = (Hls)obj;
            return H == other.H
                && L == other.L
                && S == other.S;
        }

        public static bool operator ==(Hls left, Hls right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Hls left, Hls right)
        {
            return !(left == right);
        }
    }
}
