using ColorHelper;
using System;
using System.Numerics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.util
{
    class Color
    {
        public static uint ToInt(byte r, byte g, byte b)
        {
            return (uint)(((r / 36 * 36) << 16) | ((g / 36 * 36) << 8) | (b / 85 * 85));
        }

        public static Vector3 Saturate(Vector3 rgb, float saturation)
        {

            double delta, min;
            double h = 0, S, V;

            min = Math.Min(Math.Min(rgb.X, rgb.Y), rgb.Z);
            V = Math.Max(Math.Max(rgb.X, rgb.Y), rgb.Z);
            delta = V - min;

            if (V == 0.0)
                S = 0;
            else
                S = delta / V;

            if (S == 0)
                h = 0.0;

            else
            {
                if (rgb.X == V)
                    h = (rgb.Y - rgb.Z) / delta;
                else if (rgb.Y == V)
                    h = 2 + (rgb.Z - rgb.X) / delta;
                else if (rgb.Z == V)
                    h = 4 + (rgb.X - rgb.Y) / delta;

                h *= 60;

                if (h < 0.0)
                    h = h + 360;
            }

            S = Math.Clamp(S * saturation, 0, 1);
            

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }

            return new Vector3((float)R, (float)G, (float)B);
        }

        public static Vector3 SetValue(Vector3 rgb, float value)
        {

            double delta, min;
            double h = 0, S, V;

            min = Math.Min(Math.Min(rgb.X, rgb.Y), rgb.Z);
            V = Math.Max(Math.Max(rgb.X, rgb.Y), rgb.Z);
            delta = V - min;

            if (V == 0.0)
                S = 0;
            else
                S = delta / V;

            if (S == 0)
                h = 0.0;

            else
            {
                if (rgb.X == V)
                    h = (rgb.Y - rgb.Z) / delta;
                else if (rgb.Y == V)
                    h = 2 + (rgb.Z - rgb.X) / delta;
                else if (rgb.Z == V)
                    h = 4 + (rgb.X - rgb.Y) / delta;

                h *= 60;

                if (h < 0.0)
                    h = h + 360;
            }

            V = value;


            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }

            return new Vector3((float)R, (float)G, (float)B);
        }

        public static uint HsvToRgb(double h, double S, double V)
        {
            S /= 255;
            V /= 255;

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }

            return ToInt((byte)(R * 255), (byte)(G * 255), (byte)(B * 255));
        }
    }
}
