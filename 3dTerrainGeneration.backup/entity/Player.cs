using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.world;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.entity
{
    class Player : EntityBase
    {
        public Player(World world) : base(world) { }

        public void Update(double deltaYaw, double deltaPitch)
        {
            yaw += deltaYaw;
            pitch += deltaPitch;

            pitch = Math.Max(Math.Min(pitch, 90), -90);


            if(health <= 0)
            {
                return;
            }

            var input = Keyboard.GetState();

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

        public Vector3 GetEyePosition()
        {
            return new Vector3((float)x, (float)y + 3, (float)z);
        }

        public double GetYaw()
        {
            return yaw;
        }
        public double GetPitch()
        {
            return pitch;
        }

        protected override void WalkSound()
        {
            world.soundManager.PlaySound(SoundType.WALK, new Vector3(0, -3, 0), false, (float)(rnd.NextDouble() / 3 + .9), true);
        }
    }
}
