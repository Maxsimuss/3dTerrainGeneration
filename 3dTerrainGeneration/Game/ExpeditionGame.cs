using _3dTerrainGeneration.Engine;
using _3dTerrainGeneration.Engine.Audio;
using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Graphics.UI;
using _3dTerrainGeneration.Engine.Options;
using _3dTerrainGeneration.Engine.World;
using _3dTerrainGeneration.Game.GameWorld;
using _3dTerrainGeneration.Game.Graphics._3D;
using _3dTerrainGeneration.Game.Graphics.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Game
{
    internal class ExpeditionGame : IGame
    {
        private World world;

        private ISceneLayer mainLayer;
        private ISceneLayer shadowLayer;

        public IWorld World => world;
        public ISceneLayer MainLayer => mainLayer;
        public ISceneLayer ShadowLayer => shadowLayer;

        public void EntryPoint()
        {
            Task.Run(RegisterSounds);
            RegisterOptions();

            world = new World();

            mainLayer = new MainSceneLayer(world);
            shadowLayer = new MainSceneLayer(world);
            UIRenderer.Instance.OpenScreen(new HUDScreen());
        }

        private void RegisterOptions()
        {
            OptionManager.Instance.RegisterCategory("World");
            OptionManager.Instance.RegisterOption("World", "View Distance", 128, 2048, 1024);
        }

        private void RegisterSounds()
        {
            AudioEngine.Instance.RegisterSound("Forest", "forest.mp3");
            AudioEngine.Instance.RegisterSound("Walk", "step-grass-0.mp3", "step-grass-1.mp3", "step-grass-2.mp3", "step-grass-3.mp3", "step-grass-4.mp3");
            AudioEngine.Instance.RegisterSound("Fire", "fire.mp3");
            AudioEngine.Instance.RegisterSound("Explosion", "explosion.mp3");
            AudioEngine.Instance.RegisterSound("ClickHigh", "click/high-0.mp3", "click/high-1.mp3", "click/high-2.mp3", "click/high-3.mp3");
            AudioEngine.Instance.RegisterSound("ClickLow", "click/low-0.mp3", "click/low-1.mp3", "click/low-2.mp3", "click/low-3.mp3");
            AudioEngine.Instance.RegisterSound("ClickConfirm", "click/confirm-0.mp3", "click/confirm-1.mp3", "click/confirm-2.mp3", "click/confirm-3.mp3");
        }
    }
}
