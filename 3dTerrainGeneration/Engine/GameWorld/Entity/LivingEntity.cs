using _3dTerrainGeneration.Engine.World;
using _3dTerrainGeneration.Engine.World.Entity;

namespace _3dTerrainGeneration.Engine.GameWorld.Entity
{
    internal abstract class LivingEntity<T> : DrawableEntity<T>
    {

        public LivingEntity(IWorld world, int id) : base(world, id)
        {
        }
    }
}
