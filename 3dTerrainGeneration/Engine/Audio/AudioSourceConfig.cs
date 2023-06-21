using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Audio
{
    internal enum PlaybackStartType
    {
        Predefined,
        Random,
    }

    internal struct AudioSourceConfig
    {
        public bool Relative = false;
        public bool Loop = false;

        public PlaybackStartType PlaybackStartType = PlaybackStartType.Predefined;
        public double PlaybackStartTime = 0;

        public AudioSourceConfig()
        {
        }
    }
}
