using _3dTerrainGeneration.Engine.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Graphics._3D.Cameras
{
    internal class DemoCameraPositionProvider : ICameraPositionProvider
    {
        private Vector3 position = new(0, 64, 0);
        private float yaw;
        private float pitch;

        public void Provide(Camera camera)
        {
            yaw += NoiseUtil.GetPerlin((float)(TimeUtil.Unix() % 3600000 / 1000D), 1);
            pitch += NoiseUtil.GetPerlin((float)(TimeUtil.Unix() % 3600000 / 1000D + 1238), 1);

            position += new Vector3(
                MathF.Cos(yaw / 180 * MathF.PI) * MathF.Cos(pitch / 180 * MathF.PI), 
                MathF.Sin(pitch / 180 * MathF.PI), 
                MathF.Sin(yaw / 180 * MathF.PI) * MathF.Cos(pitch / 180 * MathF.PI)) / 10;

            position.Y = 64;
            pitch -= pitch / 40;

            camera.Position = position;
            camera.Yaw = yaw;
            camera.Pitch = pitch;
        }
    }
}
