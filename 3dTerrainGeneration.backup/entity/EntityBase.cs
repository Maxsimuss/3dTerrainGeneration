using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.world;
using OpenTK;
using System;

namespace _3dTerrainGeneration.entity
{
    class EntityBase
    {
        protected Random rnd = new Random();
        protected double motionX, motionY, motionZ;
        public double x, y = 32, z;
        public Vector3 lastPlayPos = new Vector3();
        protected double yaw, pitch;
        protected bool isOnGround = true, NoClip = false;

        protected World world;
        protected AxisAlignedBB aabb = new AxisAlignedBB(.5, 3);

        public double health = 10;

        public EntityBase(World world)
        {
            this.world = world;
        }

        private double frameTime = 0;

        public virtual void PhisycsUpdate(double fT)
        {
            frameTime = fT;

            add(ref motionY, -50);

            multiply(ref motionX, .29);
            multiply(ref motionY, .29);
            multiply(ref motionZ, .29);
            add(ref y, motionY);

            int i = 0;
            while (aabb.isColliding(new Vector3d(x, (int)y + i, z), world)) {
                i++;
            }

            if((int)y + i >= y)
            {
                isOnGround = true;
                multiply(ref motionX, .05);
                multiply(ref motionY, .05);
                multiply(ref motionZ, .05);

                motionY = 0;
            } 
            else
            {
                isOnGround = false;
            }

            add(ref x, motionX);
            add(ref z, motionZ);

            if(isOnGround && (lastPlayPos - new Vector3((float)x, (float)y, (float)z)).LengthSquared > 2)
            {
                WalkSound();
                lastPlayPos = new Vector3((float)x, (float)y, (float)z);
            }

            y = Math.Max((int)y + i, y);
        }

        protected virtual void WalkSound()
        {
            world.soundManager.PlaySound(SoundType.WALK, new Vector3((float)x, (float)y, (float)z), false, (float)(rnd.NextDouble() / 3 + .9));
        }

        private void multiply(ref double a, double b)
        {
            a *= Math.Pow(b, frameTime);
        }

        private void add(ref double a, double b)
        {
            a += b * frameTime;
        }

        protected void MoveFacing(double offset, double speed)
        {
            double mult = 1;
            if(NoClip)
            {
                mult = 25;
            }

            if(isOnGround)
            {
                mult = 2.5;
            }

            add(ref motionX, Math.Cos(toRad(yaw + offset)) * speed * mult);
            add(ref motionZ, Math.Sin(toRad(yaw + offset)) * speed * mult);
        }

        protected void Jump(bool accel)
        {
            if (!isOnGround && !NoClip) return;

            motionY = 20;
        }

        protected void Sneak()
        {
            motionY = -15;
        }

        public void Hurt(double amount)
        {
            health -= amount;
            Console.WriteLine("new health: {0}", health);
        }

        public Vector3 GetPosition()
        {
            return new Vector3((float)x, (float)y, (float)z);
        }

        private double toRad(double angle)
        {
            return (Math.PI / 180) * angle;
        }
    }
}
