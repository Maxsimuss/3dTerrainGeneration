using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using _3dTerrainGeneration.world;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    class Demon : DrawableEntity
    {
        private double AIUpdateTimer, AttackCooldownTimer;
        private Player Target;

        private static AxisAlignedBB aabb;
        public override AxisAlignedBB Box => aabb;

        private static float scale;
        public override float Scale => scale;

        private static uint[][] mesh;
        private static InderectDraw[] draws;
        public override InderectDraw[] InderectDraws => draws;

        static Demon()
        {
            Mesh data = MeshLoader.Load("demon");
            scale = 4f / data.Height;

            aabb = new AxisAlignedBB(data.Width * scale / 2f, data.Height * scale);
            mesh = data.Data;
        }

        public Demon(World world, Vector3 position, int EntityId) : base(world, EntityType.Demon, EntityId)
        {
            if (draws == null)
            {
                draws = new InderectDraw[mesh.Length];
                for (int i = 0; i < mesh.Length; i++)
                {
                    draws[i] = World.gameRenderer.SubmitMesh(mesh[i], null);
                }
            }

            x = position.X; y = position.Y; z = position.Z; 
        }

        public override void PhisycsUpdate(double fT)
        {
            if (IsResponsible)
            {
                //if (Target != null)
                //{
                //    if ((GetPosition() - Target.GetPosition()).LengthSquared > 4)
                //    {
                //        MoveFacing(0, 20);
                //        if (rnd.NextDouble() < .05)
                //        {
                //            Jump(false);
                //        }
                //    }
                //    else if ((AttackCooldownTimer -= fT) <= 0)
                //    {
                //        AttackCooldownTimer = 1;
                //        //Target.Hurt(1);
                //    }
                //}
                //if ((AIUpdateTimer += fT) > 1)
                //{
                //    List<Player> players = world.GetPlayers();
                //    players.Sort((p1, p2) => { return (int)(((p1.GetPosition() - GetPosition()).LengthSquared - (p2.GetPosition() - GetPosition()).LengthSquared) * 2); });

                //    Target = players.First();
                //    yaw = -Math.Atan2(x - Target.x, z - Target.z) / Math.PI * 180 - 90 + rnd.NextDouble() * 90 - 45;
                //    AIUpdateTimer = 0;
                //}
            }

            base.PhisycsUpdate(fT);
        }
    }
}
