namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces
{
    internal interface IPackChunk : IChunk
    {
        /// <summary>
        /// The number of models.
        /// </summary>
        int ModelCount { get; }
    }
}