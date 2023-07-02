using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics.UI.Screens;
using _3dTerrainGeneration.Game.GameWorld.Entities;
using _3dTerrainGeneration.Game.GameWorld.Generators;

namespace _3dTerrainGeneration.Game.Graphics.UI
{
    internal class HUDScreen : BaseScreen
    {
        private BiomeGenerator biomeGenerator = new BiomeGenerator();

        public override void Render()
        {
            Player player = (Player)EntityManager.Instance.GetEntity(0);

            BiomeInfo biome = biomeGenerator.GetBiomeInfo((int)player.Position.X, (int)player.Position.Z);

            textRenderer.DrawTextWithShadowCentered(0, -.8f, .04f, string.Format("T: {0:0} H: {1:0} F: {2:0}", biome.Temperature, biome.Humidity, biome.Fertility));
        }
    }
}
