using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Chunks
{
    internal class PaletteChunk : Chunk, IPaletteChunk
    {
        public Color[] Colors { get; }

        public PaletteChunk(byte[] data) : base(data)
        {
            Colors = new Color[256];

            var formatParser = new FormatParser(Content);

            Colors = formatParser.ParseColors(Colors.Length);
        }
    }
}