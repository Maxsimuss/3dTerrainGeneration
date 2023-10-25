using System.Numerics;

namespace _3dTerrainGeneration.Engine.World.Entity
{
    internal abstract class EntityBase
    {
        public int EntityId { get; private set; } = 0;

        public float Yaw, Pitch;
        public float LastYaw, LastPitch;
        public Vector3 Position, LastPosition;
        public Vector3 Velocity;

        protected IWorld world;

        public EntityBase(IWorld world, int id)
        {
            this.world = world;
            this.EntityId = id;
        }

        public virtual void Tick()
        {
            LastPosition = Position;
            LastPitch = Pitch;
            LastYaw = Yaw;
        }
    }
}
