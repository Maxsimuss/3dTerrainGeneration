using _3dTerrainGeneration.Engine.Physics;
using _3dTerrainGeneration.Engine.World;
using _3dTerrainGeneration.Engine.World.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.GameWorld.Entity
{
    internal abstract class LivingEntity<T> : DrawableEntity<T>
    {

        public LivingEntity(IWorld world, int id) : base(world, id)
        {
        }
    }
}
