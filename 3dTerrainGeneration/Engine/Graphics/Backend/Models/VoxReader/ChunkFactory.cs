using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Chunks;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces;
using System;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader
{
    internal static class ChunkFactory
    {
        public static IChunk Parse(byte[] data)
        {
            ChunkType id = Chunk.GetChunkId(data);

            switch (id)
            {
                case ChunkType.Main:
                    return new Chunk(data);
                case ChunkType.Pack:
                    return new PackChunk(data);
                case ChunkType.Size:
                    return new SizeChunk(data);
                case ChunkType.Voxel:
                    return new VoxelChunk(data);
                case ChunkType.Palette:
                    return new PaletteChunk(data);
                case ChunkType.TransformNode:
                    return new TransformNodeChunk(data);
                case ChunkType.GroupNode:
                    return new GroupNodeChunk(data);
                case ChunkType.ShapeNode:
                    return new ShapeNodeChunk(data);
                case ChunkType.MaterialOld:
                case ChunkType.MaterialNew:
                case ChunkType.Layer:
                case ChunkType.Object:
                case ChunkType.Camera:
                case ChunkType.Note:
                    return new Chunk(data);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}