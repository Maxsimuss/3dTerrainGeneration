using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Options
{
    internal class GraphicsOptions : OptionBase
    {
        public int SSAO_SPP = 1;
        public int View_Distance = 512;

        public bool RTGI = false;
        public int RTGI_Resolution = 512;
        public int RTGI_SPP = 2;
    }
}
