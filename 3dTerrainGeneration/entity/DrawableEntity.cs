using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.network;
using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using _3dTerrainGeneration.world;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network;

namespace _3dTerrainGeneration.entity
{
    public abstract class DrawableEntity : EntityBase
    {
        public bool Visible = true;

        public virtual float Scale { get => 1; }

        public int EntityId;
        protected int AnimationFrame = 0;
        private Vector3 LastAnimationPosition;
        protected double NetworkPositionUpdateTimer, AnimationResetTimer;
        protected double DistanceTraveled = 0;

        public DrawableEntity(World world, EntityType type, int EntityId = -1) : base(world, type)
        {
            this.EntityId = EntityId;
        }

        protected virtual void UpdateAnimation(double fT)
        {
            DistanceTraveled += (LastAnimationPosition - GetPosition()).Length;
            LastAnimationPosition = GetPosition();

            if (DistanceTraveled > 2 && isOnGround)
            {
                DistanceTraveled = DistanceTraveled % 2;
                AnimationFrame = MathHelper.Clamp(1 - AnimationFrame, 0, 1);
                Window.Instance.SoundManager.PlaySound(SoundType.Walk, false, rnd.NextSingle() / 2 + .75f, .1f);
            }
            else
            {
                if ((AnimationResetTimer += fT) > .5)
                {
                    AnimationFrame = 0;
                    AnimationResetTimer = 0;
                }
            }
        }

        public override void PhisycsUpdate(double fT)
        {
            base.PhisycsUpdate(fT);

            UpdateAnimation(fT);

            if(IsResponsible && (NetworkPositionUpdateTimer += fT) >= .05)
            {
                world.network.UpdateEntity(EntityId, x, y, z, motionX, motionY, motionZ, yaw);
                NetworkPositionUpdateTimer = 0;
            }
        }

        public Vector3 GetPositionInterpolated(double frameDelta)
        {
            return new((float)(x * frameDelta + prevX * (1 - frameDelta)), (float)(y * frameDelta + prevY * (1 - frameDelta)), (float)(z * frameDelta + prevZ * (1 - frameDelta)));
        }

        public virtual Matrix4 GetModelMatrix(double frameDelta)
        {
            return Matrix4.CreateScale(Scale) * Matrix4.CreateTranslation((float)-Box.width, 0, (float)-Box.width) * Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(-pitch)) * Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(-yaw)) * Matrix4.CreateTranslation(GetPositionInterpolated(frameDelta));
        }

        public virtual void Render(InstancedRenderer renderer, double frameDelta)
        {
            if (!Visible) return;

            renderer.Submit(type, AnimationFrame, GetModelMatrix(frameDelta));
        }
    }
}
