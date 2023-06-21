using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Chunks;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces;
using _3dTerrainGeneration.Engine.Util;

namespace _3dTerrainGeneration.Game.GameWorld.Structures
{
    class ImportedStructure : Structure
    {
        public ImportedStructure(string fileName)
        {
            IVoxFile file = VoxReader.Read("Resources/models/" + fileName);

            VoxelChunk voxelChunk = null;
            PaletteChunk palleteChunk = null;
            for (int i = 0; i < file.Chunks.Length; i++)
            {
                if (file.Chunks[i].Type == ChunkType.Voxel)
                {
                    voxelChunk = (VoxelChunk)file.Chunks[i];
                }
                if (file.Chunks[i].Type == ChunkType.Palette)
                {
                    palleteChunk = (PaletteChunk)file.Chunks[i];
                }
            }

            for (int i = 0; i < voxelChunk.Voxels.Length; i++)
            {
                RawVoxel vox = voxelChunk.Voxels[i];
                //if (vox.ColorIndex == 0) continue;

                Engine.Graphics.Backend.Models.VoxReader.Color color = palleteChunk.Colors[vox.ColorIndex - 1];

                SetBlock(vox.Position.X, vox.Position.Z, vox.Position.Y,
                    Engine.Util.Color.ToInt(color.R, color.G, color.B));
            }

            Mesh();
        }
    }
}
