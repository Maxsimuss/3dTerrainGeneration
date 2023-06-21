using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Physics;
using _3dTerrainGeneration.Engine.World.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TerrainServer.network;

namespace _3dTerrainGeneration.Game.GameWorld.Entities
{
    class BlueSlime : LivingEntity<BlueSlime>
    {
        private double AIUpdateTimer, AttackCooldownTimer;
        private EntityBase Target;

        static BlueSlime()
        {
            MeshedModel data = ModelLoader.Load("blue-slime");
            MeshScale = 1f / data.Height;

            AABB = new AxisAlignedBB(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public BlueSlime(World world, Vector3 position, int EntityId) : base(world, EntityId)
        {
            //if (draws == null)
            //{
            //    draws = new InderectDraw[mesh.Length];
            //    for (int i = 0; i < mesh.Length; i++)
            //    {
            //        draws[i] = World.gameRenderer.SubmitMesh(mesh[i], null);
            //    }
            //}

            //x = position.X; y = position.Y; z = position.Z;
        }

        double NextAIUpdateTime = 0;

        public override void Tick()
        {
            //if (IsResponsible)
            //{
            //    AIUpdateTimer += fT;

            //    if (Target == null)
            //    {
            //        if (AIUpdateTimer > NextAIUpdateTime)
            //        {
            //            physicsData.Yaw = RANDOM.Next(0, 360);
            //            NextAIUpdateTime = RANDOM.NextDouble() * 25 + 5;
            //            AIUpdateTimer = 0;
            //        }
            //        //MoveFacing(0, 5);
            //    }

            //    List<EntityBase> players = world.GetEntities(EntityType.Player);
            //    players.Sort((p1, p2) => { return (int)(((p1.GetPosition() - GetPosition()).LengthSquared() - (p2.GetPosition() - GetPosition()).LengthSquared()) * 2); });

            //    EntityBase p = players.First();
            //    Vector3 d = p.GetPosition() - GetPosition();
            //    if (d.Length() < 10)
            //    {
            //        Target = p;

            //        physicsData.Yaw = OpenTK.Mathematics.MathHelper.RadiansToDegrees(-MathF.Atan2(d.X, d.Z)) + 90;
            //    }
            //    else
            //    {
            //        Target = null;
            //    }
            //}

            base.Tick();
        }
    }
}
