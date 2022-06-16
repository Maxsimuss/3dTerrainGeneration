using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using _3dTerrainGeneration.world;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    class Frog : DrawableEntity
    {
        private double AIUpdateTimer, AttackCooldownTimer;
        private Player Target;

        static AxisAlignedBB aabb;
        static float scale;

        public override float Scale => scale;
        public override AxisAlignedBB Box => aabb;

        public static ushort[][] mesh;
        static Frog()
        {
            Mesh data = MeshLoader.Load("frog");
            scale = 1f / data.Height;

            aabb = new AxisAlignedBB(data.Width * scale / 2f, data.Height * scale);
            mesh = data.Data;
        }

        public Frog(World world, Vector3 position, int EntityId) : base(world, EntityType.Frog, EntityId)
        {
            x = position.X; y = position.Y; z = position.Z; 
        }

        protected override void UpdateAnimation(double fT)
        {
            if(!isOnGround && motionY > 0)
                AnimationFrame = 2;
            else
                base.UpdateAnimation(fT);
        }

        double JumpTimer = 0;

        public override void PhisycsUpdate(double fT)
        {
            if (IsResponsible)
            {
                if((JumpTimer -= fT) < 0 && isOnGround)
                {
                    yaw += rnd.NextDouble() * 180 - 90;
                    Jump(true);
                    JumpTimer = rnd.NextDouble() * 4;
                }
            }

            base.PhisycsUpdate(fT);
        }
    }
}
