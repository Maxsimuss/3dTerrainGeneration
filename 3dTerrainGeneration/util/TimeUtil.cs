using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
