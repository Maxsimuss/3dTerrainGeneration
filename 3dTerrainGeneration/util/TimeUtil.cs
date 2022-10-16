using System;

namespace _3dTerrainGeneration.util
{
    public static class TimeUtil
    {
        public static double Unix()
        {
            return DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}
