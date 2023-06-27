using System.Numerics;

namespace _3dTerrainGeneration.Engine.World
{
    internal interface IWorld
    {
        public Vector3 SunPosition { get; }

        public void Tick(Vector3 origin);

        public uint GetBlockAt(double x, double y, double z);
    }
}
