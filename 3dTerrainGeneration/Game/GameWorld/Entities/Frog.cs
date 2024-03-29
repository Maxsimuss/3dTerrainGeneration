﻿using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Physics;

namespace _3dTerrainGeneration.Game.GameWorld.Entities
{
    class Frog : LivingEntity<Frog>
    {
        static Frog()
        {
            MeshedModel data = ModelLoader.Load("frog");
            MeshScale = 1f / data.Height;

            AABB = new AxisAlignedBBPrototype(data.Width * MeshScale / 2f, data.Height * MeshScale);
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
            LastPosition = Position;
            //if ((JumpTimer -= fT) < 0 && isOnGround)
            //{
            //    physicsData.Yaw += RANDOM.NextSingle() * 180 - 90;
            //    Jump(true);
            //    JumpTimer = RANDOM.NextDouble() * 4;
            //}
        }
    }
}
