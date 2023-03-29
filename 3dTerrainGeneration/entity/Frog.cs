using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.world;
using System.Numerics;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    class Frog : DrawableEntity<Frog>
    {
        private double AIUpdateTimer, AttackCooldownTimer;
        private Player Target;

        static Frog()
        {
            Mesh data = MeshLoader.Load("frog");
            MeshScale = 1f / data.Height;

            AABB = new AxisAlignedBB(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public Frog(World world, Vector3 position, int EntityId) : base(world, EntityType.Frog, EntityId)
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
            if (!isOnGround && motionY > 0)
                AnimationFrame = 2;
            else
                base.UpdateAnimation(fT);
        }

        double JumpTimer = 0;

        public override void PhisycsUpdate(double fT)
        {
            if (IsResponsible)
            {
                if ((JumpTimer -= fT) < 0 && isOnGround)
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
