using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.world;
using OpenTK.Input;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    public class Player : DrawableEntity
    {
        private static AxisAlignedBB aabb;
        public override AxisAlignedBB Box => aabb;

        private static float scale;
        public override float Scale => scale;

        private static ushort[][] mesh;
        private static InderectDraw[] draws;
        public override InderectDraw[] InderectDraws => draws;

        static Player()
        {
            Mesh data = MeshLoader.Load("player");
            scale = 3f / data.Height;

            aabb = new AxisAlignedBB(data.Width * scale / 2f, data.Height * scale);
            mesh = data.Data;
        }

        public Player(World world) : base(world, EntityType.Player) 
        {
            if (draws == null)
            {
                draws = new InderectDraw[mesh.Length];
                for (int i = 0; i < mesh.Length; i++)
                {
                    draws[i] = World.gameRenderer.SubmitMesh(mesh[i], null);
                }
            }
        }

        public void Update(KeyboardState input, double deltaYaw, double deltaPitch, bool LMB, bool RMB, double frameDelta)
        {
            yaw += deltaYaw;
            pitch += deltaPitch;
            pitch = Math.Max(Math.Min(pitch, 90), -90);

            if (LMB)
            {
                Vector3 m = Vector3.Normalize(new((float)Math.Cos(OpenTK.MathHelper.DegreesToRadians(yaw)) * (float)Math.Cos(OpenTK.MathHelper.DegreesToRadians(pitch)), (float)Math.Sin(OpenTK.MathHelper.DegreesToRadians(pitch)), (float)Math.Sin(OpenTK.MathHelper.DegreesToRadians(yaw)) * (float)Math.Cos(OpenTK.MathHelper.DegreesToRadians(pitch))));
                m *= 120;
                Vector3 eye = GetEyePosition(frameDelta);
                world.network.SpawnEntity(EntityType.FireBall, eye.X, eye.Y, eye.Z, m.X, m.Y, m.Z);
            }

            double speed = 20;

            if (input.IsKeyDown(Key.LShift))
            {
                speed *= .25;
            }

            bool isMoving = false;
            double offset = 0;

            bool W = input.IsKeyDown(Key.W);
            bool A = input.IsKeyDown(Key.A);
            bool S = input.IsKeyDown(Key.S);
            bool D = input.IsKeyDown(Key.D);

            if(W ^ S)
            {
                if(W)
                {
                    offset = 0;
                } 
                else
                {
                    offset = 180;
                }

                isMoving = true;
            }

            if (A ^ D)
            {
                if (A)
                {
                    if(isMoving)
                    {

                        if (W)
                        {
                            offset = -45;
                        }
                        else
                        {
                            offset = -135;
                        }
                    }
                    else
                    {
                        offset = -90;
                    }
                }
                else
                {
                    if (isMoving)
                    {

                        if (W)
                        {
                            offset = 45;
                        }
                        else
                        {
                            offset = 135;
                        }
                    }
                    else
                    {
                        offset = 90;
                    }
                }

                isMoving = true;
            }

            if (isMoving)
            {
                MoveFacing(offset, speed * (input.IsKeyDown(Key.ControlLeft) ? 10 : 1));
            }

            if (input.IsKeyDown(Key.Space))
            {
                Jump(false);
            }

            if (input.IsKeyDown(Key.LShift))
            {
                Sneak();
            }
        }

        public override Matrix4x4 GetModelMatrix(double frameDelta)
        {
            return Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateTranslation((float)-Box.width, 0, (float)-Box.width) * Matrix4x4.CreateRotationY((float)OpenTK.MathHelper.DegreesToRadians(-yaw)) * Matrix4x4.CreateTranslation(GetPositionInterpolated(frameDelta));
        }

        public Vector3 GetEyePosition(double frameDelta)
        {
            return new Vector3((float)(x * frameDelta + prevX * (1 - frameDelta)), (float)(y * frameDelta + prevY * (1 - frameDelta) + aabb.height - .7f), (float)(z * frameDelta + prevZ * (1 - frameDelta)));
        }

        public double GetYaw()
        {
            return yaw;
        }
        public double GetPitch()
        {
            return pitch;
        }
    }
}
