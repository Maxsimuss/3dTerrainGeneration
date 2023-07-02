using System;

namespace _3dTerrainGeneration.Engine.Util
{
    public static class MathUtil
    {
        //https://stackoverflow.com/a/1082938
        public static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        public static float Smoothstep(float edge0, float edge1, float x)
        {
            if (x < edge0)
                return 0;

            if (x >= edge1)
                return 1;

            // Scale/bias into [0..1] range
            x = (x - edge0) / (edge1 - edge0);

            return x * x * (3 - 2 * x);
        }


        public static float HaltonSequence(int i, int b)
        {
            float f = 1.0f;
            float r = 0.0f;

            while (i > 0)
            {
                f /= b;
                r = r + f * (i % b);
                i = (int)(Math.Floor(i / (float)(b)));
            }

            return r;
        }
    }
}
