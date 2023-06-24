using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Physics;
using _3dTerrainGeneration.Engine.World.Entity;
using System.Numerics;
using TerrainServer.network;

namespace _3dTerrainGeneration.Game.GameWorld.Entities
{
    class Frog : LivingEntity<Frog>
    {
        static Frog()
        {
            MeshedModel data = ModelLoader.Load("frog");
            MeshScale = 1f / data.Height;

            AABB = new AxisAlignedBB(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public Frog(World world, int EntityId) : base(world, EntityId)
        {
        }

        //protected override void UpdateAnimation()
        //{
        //    if (!isOnGround && physicsData.Velocity.Y > 0)
        //        AnimationFrame = 2;
        //}

        double JumpTimer = 0;

        public override void Tick()
        {
            //if ((JumpTimer -= fT) < 0 && isOnGround)
            //{
            //    physicsData.Yaw += RANDOM.NextSingle() * 180 - 90;
            //    Jump(true);
            //    JumpTimer = RANDOM.NextDouble() * 4;
            //}
        }
    }
}
