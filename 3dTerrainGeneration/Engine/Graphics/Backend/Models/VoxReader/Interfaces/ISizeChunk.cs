namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces
{
    internal interface ISizeChunk : IChunk
    {
        /// <summary>
        /// The size that is specified in the SIZE chunk.
        /// </summary>
        Vector3 Size { get; }
    }
}