using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.world;
using System;
using System.Collections.Generic;
using System.Numerics;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    class Spider : DrawableEntity<Spider>
    {
        private double AIUpdateTimer, AttackCooldownTimer;
        private Player Target;

        static Spider()
        {
            Mesh data = MeshLoader.Load("spider");
            MeshScale = 1f / data.Height;

            AABB = new AxisAlignedBB(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public Spider(World world, Vector3 position, int EntityId) : base(world, EntityType.Spider, EntityId)
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

        protected override void UpdateAnimation(double fT)
        {
            AnimationFrame = 0;
        }

        double RotationTimer = 0;

        private float Angle(EntityBase e)
        {
            float d = (float)(OpenTK.Mathematics.MathHelper.RadiansToDegrees(Math.Atan2(e.x - x, e.z - z)) - 90);
            if (d < -180)
            {
                d += 360;
            }

            float yw = (float)(yaw % 360 + d);
            if (yw > 180)
            {
                yw -= 360;
            }

            return Math.Abs(yw);
        }

        public override void PhisycsUpdate(double fT)
        {
            if (IsResponsible)
            {
                List<EntityBase> entities = world.GetEntities(EntityType.Spider);
                entities.AddRange(world.GetEntities(EntityType.Player));

                entities.Remove(this);

                bool near = false;
                for (int i = 0; i < entities.Count; i++)
                {
                    EntityBase e = entities[i];
                    if (Math.Sqrt((e.x - x) * (e.x - x) + (e.y - y) * (e.y - y) + (e.z - z) * (e.z - z)) < 4 && Angle(e) < 45)
                    {
                        near = true;
                        break;
                    }
                }

                if (!near)
                {
                    MoveFacing(0, 5);
                }

                if ((RotationTimer -= fT) < 0)
                {
                    yaw += rnd.NextDouble() * 90 - 45;
                    RotationTimer = rnd.NextDouble() * 4;
                }
            }

            base.PhisycsUpdate(fT);
        }
    }
}
