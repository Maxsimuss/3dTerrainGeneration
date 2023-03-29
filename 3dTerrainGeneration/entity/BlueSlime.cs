using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.world;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    class BlueSlime : DrawableEntity<BlueSlime>
    {
        private double AIUpdateTimer, AttackCooldownTimer;
        private EntityBase Target;

        static BlueSlime()
        {
            Mesh data = MeshLoader.Load("blue-slime");
            MeshScale = 1f / data.Height;

            AABB = new AxisAlignedBB(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public BlueSlime(World world, Vector3 position, int EntityId) : base(world, EntityType.BlueSlime, EntityId)
        {
            //if (draws == null)
            //{
            //    draws = new InderectDraw[mesh.Length];
            //    for (int i = 0; i < mesh.Length; i++)
            //    {
            //        draws[i] = World.gameRenderer.SubmitMesh(mesh[i], null);
            //    }
            //}

            x = position.X; y = position.Y; z = position.Z;
        }

        double NextAIUpdateTime = 0;

        public override void PhisycsUpdate(double fT)
        {
            if (IsResponsible)
            {
                AIUpdateTimer += fT;

                if (Target == null)
                {
                    if (AIUpdateTimer > NextAIUpdateTime)
                    {
                        yaw = rnd.Next(0, 360);
                        NextAIUpdateTime = rnd.NextDouble() * 25 + 5;
                        AIUpdateTimer = 0;
                    }
                    MoveFacing(0, 5);
                }

                List<EntityBase> players = world.GetEntities(EntityType.Player);
                players.Sort((p1, p2) => { return (int)(((p1.GetPosition() - GetPosition()).LengthSquared() - (p2.GetPosition() - GetPosition()).LengthSquared()) * 2); });

                EntityBase p = players.First();
                Vector3 d = p.GetPosition() - GetPosition();
                if (d.Length() < 10)
                {
                    Target = p;

                    yaw = OpenTK.Mathematics.MathHelper.RadiansToDegrees(-Math.Atan2(d.X, d.Z)) + 90;
                }
                else
                {
                    Target = null;
                }
            }

            base.PhisycsUpdate(fT);
        }
    }
}
