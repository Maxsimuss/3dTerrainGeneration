using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration
{
    public class GameSettings
    {
        public static readonly int CHUNK_SIZE = 64, VIEW_DISTANCE = 256, MAX_CORES = 3;
        public static readonly float VOLUME = 5.5f, SENSITIVITY = .15f;
    }
}
