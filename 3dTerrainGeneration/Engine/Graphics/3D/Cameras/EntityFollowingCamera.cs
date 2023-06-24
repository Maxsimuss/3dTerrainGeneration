using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.World.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            camera.Yaw = entity.Yaw;
            camera.Pitch = entity.Pitch;
        }
    }
}
