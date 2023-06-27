using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.World;

namespace _3dTerrainGeneration.Engine
{
    internal interface IGame
    {
        public void EntryPoint(VoxelEngine engine);

        public IWorld World { get; }
        public ISceneLayer MainLayer { get; }
        public ISceneLayer ShadowLayer { get; }
    }
}
