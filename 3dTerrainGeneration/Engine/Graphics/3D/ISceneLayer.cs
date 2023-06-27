using System.Numerics;

namespace _3dTerrainGeneration.Engine.Graphics._3D
{
    internal interface ISceneLayer
    {
        public void Render(Camera camera, Matrix4x4 matrix);
    }
}
