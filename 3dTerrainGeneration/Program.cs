using _3dTerrainGeneration.Engine;
using _3dTerrainGeneration.Game;
using OpenTK.Windowing.Desktop;
using System;
using System.Runtime.InteropServices;

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
