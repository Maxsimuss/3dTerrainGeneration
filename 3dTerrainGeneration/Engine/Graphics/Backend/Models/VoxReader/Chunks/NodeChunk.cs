using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces;
using System.Collections.Generic;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Chunks
{
    internal class NodeChunk : Chunk, INodeChunk
    {
        public int NodeId { get; }

        public IDictionary<string, string> Attributes { get; }

        protected readonly FormatParser FormatParser;

        public NodeChunk(byte[] data) : base(data)
        {
            FormatParser = new FormatParser(Content);

            NodeId = FormatParser.ParseInt32();

            Attributes = FormatParser.ParseDictionary();
        }
    }
}