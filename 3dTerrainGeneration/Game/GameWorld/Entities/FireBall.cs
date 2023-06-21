using _3dTerrainGeneration.Engine.Audio;
using _3dTerrainGeneration.Engine.Audio.Sources;
using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Physics;
using _3dTerrainGeneration.Engine.World.Entity;
using System.Numerics;
using TerrainServer.network;

namespace _3dTerrainGeneration.Game.GameWorld.Entities
{
    internal class FireBall : LivingEntity<FireBall>
    {
        public float Radius = 50;
        public bool Dead = false;

        SoundSource source;
        static FireBall()
        {
            MeshedModel data = ModelLoader.Load("fireball", 255);
            MeshScale = 2f / data.Height;

            AABB = new AxisAlignedBB(data.Width * MeshScale / 2f, data.Height * MeshScale / 2f);
            Mesh = data.Data;
        }

        public FireBall(World world, Vector3 position, Vector3 motion, int EntityId = -1) : base(world, EntityId)
        {
            //if (draws == null)
            //{
            //    draws = new InderectDraw[mesh.Length];
            //    for (int i = 0; i < mesh.Length; i++)
            //    {
            //        draws[i] = World.gameRenderer.SubmitMesh(mesh[i], null);
            //    }
            //}

            //maxHealth = 10;
            //health = maxHealth;
            //x = position.X; y = position.Y; z = position.Z;
            //motionX = motion.X;
            //motionY = motion.Y;
            //motionZ = motion.Z;

            //yaw = OpenTK.Mathematics.MathHelper.RadiansToDegrees(Math.Atan2(-motionX, motionZ));
            //pitch = OpenTK.Mathematics.MathHelper.RadiansToDegrees(Math.Atan2(motionY, Math.Sqrt(motionX * motionX + motionZ * motionZ)));

            //source = Window.Instance.SoundManager.PlaySound(SoundType.Fire, GetPosition(), true, RANDOM.NextSingle() * .2f + .9f, 5f);
        }

        public override void Tick()
        {
            //frameTime = fT;

            //prevX = x;
            //prevY = y;
            //prevZ = z;
            //x += motionX / 20;
            //y += motionY / 20;
            //z += motionZ / 20;

            //source.SetPosition(GetPosition());
            //source.SetVelocity(new((float)motionX, (float)motionY, (float)motionZ));

            //if (IsResponsible)
            //{
            //    health -= Math.Sqrt((prevX - x) * (prevX - x) + (prevY - y) * (prevY - y) + (prevZ - z) * (prevZ - z)) / 100;

            //    if (AABB.isColliding(x, y - Box.height / 2, z, world))
            //    {
            //        health -= 5;
            //    }

            //    if (health <= 0)
            //    {
            //        if (!Dead)
            //        {
            //            world.network.DespawnEntity(EntityId);
            //        }
            //        Dead = true;
            //        source.Loop = false;
            //        source.TTL = 0;
            //    }

            //    AxisAlignedBB damageBox = new AxisAlignedBB(4, 4);
            //    foreach (var item in world.GetEntities())
            //    {
            //        if (item is FireBall) continue;
            //        if (item is Player) continue;

            //        if (damageBox.isColliding(item.x - x, item.y - y, item.z - z, item.Box))
            //        {
            //            world.DespawnEntity(item.EntityId);
            //            health--;
            //        }
            //    }
            //}

            //if (IsResponsible && (NetworkPositionUpdateTimer += fT) >= .05)
            //{
            //    world.network.UpdateEntity(EntityId, x, y, z, motionX, motionY, motionZ, yaw);
            //    NetworkPositionUpdateTimer = 0;
            //}
        }

        protected override Matrix4x4 ModelMatrix =>
            Matrix4x4.CreateScale(MeshScale) * Matrix4x4.CreateTranslation((float)-AABB.width, (float)(-AABB.height / 2), (float)-AABB.width) * Matrix4x4.CreateRotationX((float)OpenTK.Mathematics.MathHelper.DegreesToRadians(-Pitch)) * Matrix4x4.CreateRotationY((float)OpenTK.Mathematics.MathHelper.DegreesToRadians(-Yaw)) * Matrix4x4.CreateTranslation(InterpolatedPosition);

        //public override void Despawn()
        //{
        //    Window.Instance.SoundManager.PlaySound(SoundType.Explosion, GetPosition(), false, RANDOM.NextSingle() * .25f + .75f, 5);
        //    source.Loop = false;
        //    source.TTL = 0;

        //    base.Despawn();
        //}

        public override void Render(float frameDelta)
        {
            if (!Dead)
                base.Render(frameDelta);
        }
    }
}
