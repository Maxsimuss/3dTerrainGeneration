using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.world;
using System.Numerics;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    class Demon : DrawableEntity<Demon>
    {
        private double AIUpdateTimer, AttackCooldownTimer;
        private Player Target;

        static Demon()
        {
            Mesh data = MeshLoader.Load("demon");
            MeshScale = 4f / data.Height;

            AABB = new AxisAlignedBB(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public Demon(World world, Vector3 position, int EntityId) : base(world, EntityType.Demon, EntityId)
        {
            //if (draws == null)
            //{
            //    draws = new InderectDraw[Mesh.Length];
            //    for (int i = 0; i < Mesh.Length; i++)
            //    {
            //        draws[i] = World.gameRenderer.SubmitMesh(Mesh[i], null);
            //    }
            //}

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
