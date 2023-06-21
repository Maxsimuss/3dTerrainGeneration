using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Physics;
using _3dTerrainGeneration.Engine.World.Entity;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Numerics;
using TerrainServer.network;

namespace _3dTerrainGeneration.Game.GameWorld.Entities
{
    internal class Player : LivingEntity<Player>
    {
        static Player()
        {
            MeshedModel data = ModelLoader.Load("player");
            MeshScale = 3f / data.Height;

            AABB = new AxisAlignedBB(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public Player(World world, int entityId) : base(world, entityId)
        {
            //if (draws == null)
            //{
            //    draws = new InderectDraw[mesh.Length];
            //    for (int i = 0; i < mesh.Length; i++)
            //    {
            //        draws[i] = World.gameRenderer.SubmitMesh(mesh[i], null);
            //    }
            //}
        }

        public void Update(KeyboardState input, double yaw, double pitch, bool LMB, bool RMB)
        {
            //if (LMB)
            //{
            //    Vector3 m = Vector3.Normalize(new((float)Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(physicsData.Yaw)) * (float)Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(physicsData.Pitch)), (float)Math.Sin(OpenTK.Mathematics.MathHelper.DegreesToRadians(pitch)), (float)Math.Sin(OpenTK.Mathematics.MathHelper.DegreesToRadians(physicsData.Yaw)) * (float)Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(pitch))));
            //    m *= 120;
            //    Vector3 eye = GetEyePosition(frameDelta);
            //    world.network.SpawnEntity(EntityType.FireBall, eye.X, eye.Y, eye.Z, m.X, m.Y, m.Z);
            //}

            float speed = 1;

            if (input.IsKeyDown(Keys.LeftControl))
            {
                speed *= 2;
            }

            bool W = input.IsKeyDown(Keys.W);
            bool A = input.IsKeyDown(Keys.A);
            bool S = input.IsKeyDown(Keys.S);
            bool D = input.IsKeyDown(Keys.D);

            bool Up = input.IsKeyDown(Keys.Space);
            bool Down = input.IsKeyDown(Keys.LeftShift);

            float moveSide = (A ? -1 : 0) + (D ? 1 : 0);
            float moveForward = (S ? -1 : 0) + (W ? 1 : 0);

            if (moveSide != 0 || moveForward != 0)
            {
                float length = MathF.Sqrt(moveSide * moveSide + moveForward * moveForward);

                moveSide /= length;
                moveForward /= length;
            }

            //MoveFacing(new(moveSide * speed, (Down ? -1 : 0) + (Up ? 1 : 0), moveForward * speed), (float)yaw, (float)pitch);

            //if (input.IsKeyDown(Keys.Space))
            //{
            //    Jump(false);
            //}

            //if (input.IsKeyDown(Keys.LeftShift))
            //{
            //    Sneak();
            //}
        }
    }
}
