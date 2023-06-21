using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Chunks
{
    internal class GroupNodeChunk : NodeChunk, IGroupNodeChunk
    {
        public int ChildrenCount => ChildrenNodes.Length;
        public int[] ChildrenNodes { get; }

        public GroupNodeChunk(byte[] data) : base(data)
        {
            int childCount = FormatParser.ParseInt32();

            ChildrenNodes = new int[childCount];
            for (int i = 0; i < childCount; i++)
            {
                ChildrenNodes[i] = FormatParser.ParseInt32();
            }
        }
    }
}