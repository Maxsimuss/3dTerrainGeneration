using _3dTerrainGeneration.Engine.World.Entity;

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
            camera.Position = entity.InterpolatedPosition;
            camera.Position.Y += entity.HitBox.height * .9f;
            camera.Yaw = entity.Yaw;
            camera.Pitch = entity.Pitch;
        }
    }
}
