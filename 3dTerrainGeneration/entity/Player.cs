using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.world;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    public class Player : DrawableEntity
    {
        public override AxisAlignedBB Box => aabb;
        public override float Scale => scale;
        static float scale;
        static AxisAlignedBB aabb;

        public static ushort[][] mesh;
        static Player()
        {
            Mesh data = MeshLoader.Load("player");
            scale = 3f / data.Height;

            aabb = new AxisAlignedBB(data.Width * scale / 2f, data.Height * scale);
            mesh = data.Data;
        }

        public Player(World world) : base(world, EntityType.Player) 
        {
        }

        public void Update(KeyboardState input, double deltaYaw, double deltaPitch, bool LMB, bool RMB, double frameDelta)
        {
            yaw += deltaYaw;
            pitch += deltaPitch;
            pitch = Math.Max(Math.Min(pitch, 90), -90);

            if (LMB)
            {
                Vector3 m = new((float)Math.Cos(MathHelper.DegreesToRadians(yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(pitch)), (float)Math.Sin(MathHelper.DegreesToRadians(pitch)), (float)Math.Sin(MathHelper.DegreesToRadians(yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(pitch)));
                m.Normalize();
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
                MoveFacing(offset, speed);
            }

            if (input.IsKeyDown(Key.Space))
            {
                Jump(input.IsKeyDown(Key.ControlLeft));
            }

            if (input.IsKeyDown(Key.LShift))
            {
                Sneak();
            }
        }

        public override Matrix4 GetModelMatrix(double frameDelta)
        {
            return Matrix4.CreateScale(Scale) * Matrix4.CreateTranslation((float)-Box.width, 0, (float)-Box.width) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(-yaw)) * Matrix4.CreateTranslation(GetPositionInterpolated(frameDelta));
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
