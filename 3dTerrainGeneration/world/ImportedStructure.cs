using VoxReader;

namespace _3dTerrainGeneration.world
{
    class ImportedStructure : Structure
    {
        public ImportedStructure(string fileName)
        {
            VoxReader.Interfaces.IVoxFile file = VoxReader.VoxReader.Read("Resources/models/" + fileName);

            VoxReader.Chunks.VoxelChunk voxelChunk = null;
            VoxReader.Chunks.PaletteChunk palleteChunk = null;
            for (int i = 0; i < file.Chunks.Length; i++)
            {
                if (file.Chunks[i].Type == ChunkType.Voxel)
                {
                    voxelChunk = (VoxReader.Chunks.VoxelChunk)file.Chunks[i];
                }
                if (file.Chunks[i].Type == ChunkType.Palette)
                {
                    palleteChunk = (VoxReader.Chunks.PaletteChunk)file.Chunks[i];
                }
            }

            for (int i = 0; i < voxelChunk.Voxels.Length; i++)
            {
                RawVoxel vox = voxelChunk.Voxels[i];
                //if (vox.ColorIndex == 0) continue;

                Color color = palleteChunk.Colors[vox.ColorIndex - 1];

                SetBlock(vox.Position.X, vox.Position.Z, vox.Position.Y,
                    util.Color.ToInt(color.R, color.G, color.B));
            }

            Mesh();
        }
    }
}
