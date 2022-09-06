using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using _3dTerrainGeneration.world;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    class BlueSlime : DrawableEntity
    {
        private double AIUpdateTimer, AttackCooldownTimer;
        private DrawableEntity Target;

        private static AxisAlignedBB aabb;
        public override AxisAlignedBB Box => aabb;
        
        private static float scale;
        public override float Scale => scale;

        private static uint[][] mesh;
        private static InderectDraw[] draws;
        public override InderectDraw[] InderectDraws => draws;

        static BlueSlime()
        {
            Mesh data = MeshLoader.Load("blue-slime");
            scale = 1f / data.Height;

            aabb = new AxisAlignedBB(data.Width * scale / 2f, data.Height * scale);
            mesh = data.Data;
        }

        public BlueSlime(World world, Vector3 position, int EntityId) : base(world, EntityType.BlueSlime, EntityId)
        {
            if(draws == null)
            {
                draws = new InderectDraw[mesh.Length];
                for (int i = 0; i < mesh.Length; i++)
                {
                    draws[i] = World.gameRenderer.SubmitMesh(mesh[i], null);
                }
            }

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

                List<DrawableEntity> players = world.GetEntities(EntityType.Player);
                players.Sort((p1, p2) => { return (int)(((p1.GetPosition() - GetPosition()).LengthSquared() - (p2.GetPosition() - GetPosition()).LengthSquared()) * 2); });

                DrawableEntity p = players.First();
                Vector3 d = p.GetPosition() - GetPosition();
                if (d.Length() < 10)
                {
                    Target = p;

                    yaw = OpenTK.MathHelper.RadiansToDegrees(-Math.Atan2(d.X, d.Z)) + 90;
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
