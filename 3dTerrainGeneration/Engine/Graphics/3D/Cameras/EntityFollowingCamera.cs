using _3dTerrainGeneration.Engine.World.Entity;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Graphics._3D.Cameras
{
    internal class EntityFollowingCamera<T> : ICameraPositionProvider
    {
        DrawableEntity<T> entity;

        public EntityFollowingCamera(DrawableEntity<T> entity)
        {
            this.entity = entity;
        }

        public void Provide(Camera camera)
        {
            camera.Position = Vector3.Transform(new(),
                Matrix4x4.CreateTranslation(-5, 0, 0) *
                Matrix4x4.CreateRotationZ(OpenTK.Mathematics.MathHelper.DegreesToRadians(entity.Pitch)) *
                Matrix4x4.CreateRotationY(OpenTK.Mathematics.MathHelper.DegreesToRadians(-entity.Yaw + 75)) *
                Matrix4x4.CreateTranslation(entity.InterpolatedPosition)
            );
            camera.Position.Y += entity.HitBox.height * .5f;
            camera.Yaw = entity.Yaw - 75;
            camera.Pitch = entity.Pitch;
        }
    }
}
