using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.world;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    public class FireBall : DrawableEntity
    {
        public override AxisAlignedBB Box => aabb;
        public override float Scale => scale;

        public float Radius = 50;

        static float scale;
        static AxisAlignedBB aabb;
        public bool Dead = false;

        public static ushort[][] mesh;

        SoundSource source;
        static FireBall()
        {
            Mesh data = MeshLoader.Load("fireball", 255);
            scale = 2f / data.Height;

            aabb = new AxisAlignedBB(data.Width * scale / 2f, data.Height * scale / 2f);
            mesh = data.Data;
        }

        public FireBall(World world, Vector3 position, Vector3 motion, int EntityId = -1) : base(world, EntityType.FireBall, EntityId)
        {
            maxHealth = 10;
            health = maxHealth;
            x = position.X; y = position.Y; z = position.Z;
            motionX = motion.X;
            motionY = motion.Y;
            motionZ = motion.Z;

            yaw = MathHelper.RadiansToDegrees(Math.Atan2(-motionX, motionZ));
            pitch = MathHelper.RadiansToDegrees(Math.Atan2(motionY, Math.Sqrt(motionX * motionX + motionZ * motionZ)));

            source = Window.Instance.SoundManager.PlaySound(audio.SoundType.Fire, GetPosition(), true, rnd.NextSingle() * .2f + .9f, 5f);
        }

        public override void PhisycsUpdate(double fT)
        {
            frameTime = fT;

            prevX = x;
            prevY = y;
            prevZ = z;
            x += motionX / 20;
            y += motionY / 20;
            z += motionZ / 20;

            source.SetPosition(GetPosition());
            source.SetVelocity(new((float)motionX, (float)motionY, (float)motionZ));

            if (IsResponsible)
            {
                health -= Math.Sqrt((prevX - x) * (prevX - x) + (prevY - y) * (prevY - y) + (prevZ - z) * (prevZ - z)) / 100;

                if (aabb.isColliding(x, y - Box.height / 2, z, world))
                {
                    health -= 5;
                }

                if (health <= 0)
                {
                    if (!Dead)
                    {
                        world.network.DespawnEntity(EntityId);
                    }
                    Dead = true;
                    source.Loop = false;
                    source.TTL = 0;
                }

                AxisAlignedBB damageBox = new AxisAlignedBB(4, 4);
                foreach (var item in world.GetEntities())
                {
                    if (item is FireBall) continue;
                    if (item is Player) continue;
                    
                    if(damageBox.isColliding(item.x - x, item.y - y, item.z - z, item.Box))
                    {
                        world.DespawnEntity(item.EntityId);
                        health--;
                    }
                }
            }

            if (IsResponsible && (NetworkPositionUpdateTimer += fT) >= .05)
            {
                world.network.UpdateEntity(EntityId, x, y, z, motionX, motionY, motionZ, yaw);
                NetworkPositionUpdateTimer = 0;
            }
        }

        public override Matrix4 GetModelMatrix(double frameDelta)
        {
            return Matrix4.CreateScale(Scale) * Matrix4.CreateTranslation((float)-Box.width, (float)(-Box.height / 2), (float)-Box.width) * Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(-pitch)) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(-yaw)) * Matrix4.CreateTranslation(GetPositionInterpolated(frameDelta));
        }

        public override void Despawn()
        {
            Window.Instance.SoundManager.PlaySound(SoundType.Explosion, GetPosition(), false, rnd.NextSingle() * .25f + .75f, 5);
            source.Loop = false;
            source.TTL = 0;

            base.Despawn();
        }

        public override void Render(InstancedRenderer renderer, double frameDelta)
        {
            if(!Dead)
                base.Render(renderer, frameDelta);
        }
    }
}
