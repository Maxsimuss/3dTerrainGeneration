using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Chunks
{
    internal class SizeChunk : Chunk, ISizeChunk
    {
        public Vector3 Size { get; }

        public SizeChunk(byte[] data) : base(data)
        {
            var formatParser = new FormatParser(Content);

            Size = formatParser.ParseVector3();
        }

        public override string ToString()
        {
            return $"{base.ToString()}, {Size}";
        }
    }
}