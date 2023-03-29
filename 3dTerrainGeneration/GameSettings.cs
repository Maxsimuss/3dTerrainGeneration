using System;

namespace _3dTerrainGeneration
{
    public class GameSettings
    {
        private static GameSettings instance = null;
        public static GameSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameSettings();
                }

                return instance;
            }
        }

        public int SSAO_SPP = 1;
        public int View_Distance = 256;

        public bool RTGI = false;
        public int RTGI_Resolution = 512;
        public int RTGI_SPP = 2;

        public float Volume = 5.5f;
        public float MouseSensitivity = .15f;

        public static readonly int CHUNK_SIZE = 64, MAX_CORES = Math.Max(Environment.ProcessorCount - 2, 1);
    }
}
