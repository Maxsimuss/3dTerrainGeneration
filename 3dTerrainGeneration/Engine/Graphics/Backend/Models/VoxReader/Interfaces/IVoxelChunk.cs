namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces
{
    internal interface IVoxelChunk : IChunk
    {
        /// <summary>
        /// All voxels that are contained in the XYZI chunk.
        /// </summary>
        RawVoxel[] Voxels { get; }
    }
}