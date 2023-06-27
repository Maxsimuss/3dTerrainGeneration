﻿using _3dTerrainGeneration.Engine;
using _3dTerrainGeneration.Engine.Audio;
using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics;
using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Graphics._3D.Cameras;
using _3dTerrainGeneration.Engine.Graphics.UI;
using _3dTerrainGeneration.Engine.Options;
using _3dTerrainGeneration.Engine.World;
using _3dTerrainGeneration.Game.GameWorld;
using _3dTerrainGeneration.Game.GameWorld.Entities;
using _3dTerrainGeneration.Game.Graphics._3D;
using _3dTerrainGeneration.Game.Graphics.UI;
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

        public void EntryPoint(VoxelEngine engine)
        {
            Task.Run(RegisterSounds);
            RegisterOptions();

            world = new World();

            Player player = new Player(world, EntityManager.Instance.GetNextEntityId());
            player.Visible = false;
            EntityManager.Instance.AddEntity(player);

            for (int i = 0; i < 10; i++)
            {
                EntityManager.Instance.AddEntity(new Frog(world, EntityManager.Instance.GetNextEntityId()));
            }

            engine.UserInputHandler.RegisterInputHandler(player);
            GraphicsEngine.Instance.CameraPositionProvider = new EntityFollowingCamera<Player>(player);

            mainLayer = new MainSceneLayer(world);
            shadowLayer = new MainSceneLayer(world);
            UIRenderer.Instance.OpenScreen(new HUDScreen());
        }

        private void RegisterOptions()
        {
            OptionManager.Instance.RegisterOption("Audio", "Volume", new DoubleOption(0, 10, 5));
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
