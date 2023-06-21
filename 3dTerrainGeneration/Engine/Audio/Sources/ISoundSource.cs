using System.Numerics;

namespace _3dTerrainGeneration.Engine.Audio.Sources
{
    internal interface ISoundSource
    {
        public void Play();
        public void Stop();

        public double TTL { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
    }
}
