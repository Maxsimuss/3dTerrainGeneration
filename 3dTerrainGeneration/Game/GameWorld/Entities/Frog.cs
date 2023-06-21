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
        private double AIUpdateTimer, AttackCooldownTimer;
        private Player Target;

        static Frog()
        {
            MeshedModel data = ModelLoader.Load("frog");
            MeshScale = 1f / data.Height;

            AABB = new AxisAlignedBB(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public Frog(World world, Vector3 position, int EntityId) : base(world, EntityId)
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

        //protected override void UpdateAnimation(double fT)
        //{
        //    if (!isOnGround && physicsData.Velocity.Y > 0)
        //        AnimationFrame = 2;
        //    else
        //        base.UpdateAnimation(fT);
        //}

        double JumpTimer = 0;

        public override void Tick()
        {
            //if (IsResponsible)
            //{
            //    if ((JumpTimer -= fT) < 0 && isOnGround)
            //    {
            //        physicsData.Yaw += RANDOM.NextSingle() * 180 - 90;
            //        Jump(true);
            //        JumpTimer = RANDOM.NextDouble() * 4;
            //    }
            //}

            //base.PhisycsUpdate(fT);
        }
    }
}
