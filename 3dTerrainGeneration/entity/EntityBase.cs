using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.world;
using OpenTK;
using System;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    public class EntityBase
    {
        static AxisAlignedBB DefaultAABB = new AxisAlignedBB(.5, 3);

        public static Random rnd = new Random();
        public double motionX, motionY, motionZ, jumpMotion = 20;
        public double x, y = 32, z;
        public double prevX, prevY = 32, prevZ;
        public Vector3 lastPlayPos;
        public double yaw, pitch;
        protected bool isOnGround = true;
        public bool IsResponsible;
        public EntityType type;

        protected World world;
        public virtual AxisAlignedBB Box { get => DefaultAABB; }


        public double maxHealth = 300;
        public double health;

        public EntityBase(World world, EntityType type)
        {
            this.type = type;
            this.world = world;
            health = maxHealth;
            lastPlayPos = new Vector3((float)x, (float)y, (float)z);
        }

        protected double frameTime = 0;

        public virtual void PhisycsUpdate(double fT)
        {
            prevX = x;
            prevY = y;
            prevZ = z; 
            frameTime = fT;

            if(true)
            {
                motionX *= .8;
                motionY *= .8;
                motionZ *= .8;

                x += motionX / 10;
                y += motionY * 2;
                z += motionZ / 10;
            }
            else
            {
                motionY -= .15;
                motionY *= .96;

                if (isOnGround)
                {
                    motionX *= .15;
                    motionZ *= .15;
                }
                else
                {
                    motionX *= .8;
                    motionZ *= .8;
                }

                double mx = Math.Abs(motionX);
                double my = Math.Abs(motionY);
                double mz = Math.Abs(motionZ);
                int subdevision = (int)Math.Ceiling(Math.Max(Math.Max(mx, my), mz) * 2);

                double _motionX = motionX / subdevision;
                double _motionY = motionY / subdevision;
                double _motionZ = motionZ / subdevision;
                for (int i = 0; i < subdevision; i++)
                {
                    if (!Box.isColliding(x + _motionX, y, z, world))
                    {
                        x += _motionX;
                    }
                    else
                    {
                        double _m = 0;
                        double step = _motionX > 0 ? .01 : -.01;
                        while ((!Box.isColliding(x + _m, y, z, world) || !Box.isColliding(x + _m, y + 1, z, world)) && Math.Abs(_m) < Math.Abs(_motionX))
                        {
                            if(Box.isColliding(x + _m, y, z, world) && !Box.isColliding(x + _m, y + 1, z, world))
                            {
                                y++;
                            }
                            _m += step;
                        }
                        x += _m - step;
                    }

                    if (!Box.isColliding(x, y + _motionY, z, world))
                    {
                        y += _motionY;
                        isOnGround = false;
                    }
                    else
                    {
                        if (_motionY < 0)
                        {
                            y = Math.Round(y + _motionY / 2);
                        }
                        else
                        {
                            y = Math.Round(y + _motionY / 2 - Box.height) + Box.height;
                        }
                        motionY = 0;
                        isOnGround = true;
                    }

                    if (!Box.isColliding(x, y, z + _motionZ, world))
                    {
                        z += _motionZ;
                    }
                    else
                    {
                        double _m = 0;
                        double step = _motionZ > 0 ? .01 : -.01;
                        while ((!Box.isColliding(x, y, z + _m, world) || !Box.isColliding(x, y + 1, z + _m, world)) && Math.Abs(_m) < Math.Abs(_motionZ))
                        {
                            if (Box.isColliding(x, y, z + _m, world) && !Box.isColliding(x, y + 1, z + _m, world))
                            {
                                y++;
                            }
                            _m += step;
                        }
                        z += _m - step;
                    }

                    if (Box.isColliding(x, y, z, world))
                    {
                        x += _motionX;
                        y += _motionY;
                        z += _motionZ;

                        //_motionX /= 10;
                        //_motionY /= 10;
                        //_motionZ /= 10;
                    }
                }
            }
        }

        public virtual void MoveFacing(double offset, double speed, bool global = false)
        {
            double mult = .05;

            if (isOnGround)
            {
                mult = 2;
            }

            if (global)
            {
                motionX += Math.Cos(toRad(offset)) * speed * mult / 20;
                motionZ += Math.Sin(toRad(offset)) * speed * mult / 20;
            }
            else
            {
                motionX += Math.Cos(toRad(yaw + offset)) * speed * mult / 20;
                motionZ += Math.Sin(toRad(yaw + offset)) * speed * mult / 20;
            }
        }

        protected void Jump(bool accel)
        {
            if (!isOnGround) return;

            if (accel)
            {
                motionX += Math.Cos(toRad(yaw)) * 10;
                motionZ += Math.Sin(toRad(yaw)) * 10;
            }
            motionY = 1;
            //y++;
        }

        protected void Sneak()
        {
            motionY = -1;
        }

        public void Hurt(double amount)
        {
            health -= amount;
            System.Diagnostics.Debug.WriteLine("new health: {0}", health);
        }

        public Vector3 GetPosition()
        {
            return new Vector3((float)x, (float)y, (float)z);
        }

        private double toRad(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        public virtual void Despawn()
        {

        }
    }


    public static class EntityTypeExtensions
    {
        public static DrawableEntity GetEntity(this EntityType entityType, World world, Vector3 pos, Vector3 motion, int id)
        {
            switch (entityType)
            {
                case EntityType.Player:
                    return new Player(world);
                case EntityType.BlueSlime:
                    return new BlueSlime(world, pos, id);
                case EntityType.Demon:
                    return new Demon(world, pos, id);
                case EntityType.Frog:
                    return new Frog(world, pos, id);
                case EntityType.Spider:
                    return new Spider(world, pos, id);
                case EntityType.FireBall:
                    return new FireBall(world, pos, motion, id);
                default:
                    return null;
            }
        }
    }
}
