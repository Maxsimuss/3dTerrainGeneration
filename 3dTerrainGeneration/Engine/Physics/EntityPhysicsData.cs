using System.Numerics;

namespace _3dTerrainGeneration.Engine.Physics
{
    public struct EntityPhysicsData
    {
        public Vector3 Position, LastPosition, Velocity;
        public float Yaw, Pitch;
        public float LastYaw, LastPitch;
    }
}
