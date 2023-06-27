using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Game.GameWorld;
using System.Numerics;

namespace _3dTerrainGeneration.Game.Graphics._3D
{
    internal class MainSceneLayer : ISceneLayer
    {
        World world = null;

        public MainSceneLayer(World world)
        {
            this.world = world;
        }

        public void Render(Camera camera, Matrix4x4 matrix)
        {
            world.Render(camera);
            EntityManager.Instance.Render();
        }
    }
}
