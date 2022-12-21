using System;

namespace _3dTerrainGeneration
{
    public class GameSettings
    {
        public int SSAO_SPP = 1;
        public int View_Distance = 512;

        public bool RTGI = false;
        public int RTGI_Resolution = 512;
        public int RTGI_SPP = 2;

        public float Volume = 5.5f;
        public float MouseSensitivity = .15f;

        public static readonly int CHUNK_SIZE = 96, MAX_CORES = Math.Max(Environment.ProcessorCount - 2, 1);
    }
}
