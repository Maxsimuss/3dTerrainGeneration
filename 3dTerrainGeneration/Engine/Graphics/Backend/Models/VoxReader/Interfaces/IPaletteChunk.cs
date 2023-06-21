namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces
{
    internal interface IPaletteChunk : IChunk
    {
        /// <summary>
        /// The colors stored in the RGBA chunk.
        /// </summary>
        Color[] Colors { get; }
    }
}