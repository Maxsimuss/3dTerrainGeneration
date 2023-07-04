using _3dTerrainGeneration.Engine.Util;
using VoxReader;
using VoxReader.Interfaces;

namespace _3dTerrainGeneration.Game.GameWorld.Structures
{
    class ImportedStructure : Structure
    {
        public ImportedStructure(string fileName)
        {
            IVoxFile file = VoxReader.VoxReader.Read(ResourceManager.GetStructure(fileName));

            IModel voxModel = file.Models[0];
            for (int i = 0; i < voxModel.Voxels.Length; i++)
            {
                Voxel vox = voxModel.Voxels[i];
                //if (vox.ColorIndex == 0) continue;

                SetBlock(vox.Position.X, vox.Position.Z, vox.Position.Y, Engine.Util.Color.ToInt(vox.Color.R, vox.Color.G, vox.Color.B));
            }

            Mesh();
        }
    }
}
