using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Chunks
{
    internal class PackChunk : Chunk, IPackChunk
    {
        public int ModelCount { get; }

        public PackChunk(byte[] data) : base(data)
        {
            var formatParser = new FormatParser(Content);

            ModelCount = formatParser.ParseInt32();
        }
    }
}