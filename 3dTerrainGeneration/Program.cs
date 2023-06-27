using _3dTerrainGeneration.Engine;
using _3dTerrainGeneration.Game;

namespace _3dTerrainGeneration
{
    public static class Program
    {
        private static void Main()
        {
            ExpeditionGame game = new ExpeditionGame();
            VoxelEngine engine = new VoxelEngine(game);
            engine.Run();
        }
    }
}
