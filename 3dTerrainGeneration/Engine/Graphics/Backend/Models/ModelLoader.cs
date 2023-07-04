using _3dTerrainGeneration.Engine.Util;
using System.Collections.Generic;
using System.IO;
using VoxReader;
using VoxReader.Interfaces;
using Color = VoxReader.Color;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models
{
    internal class ModelLoader
    {
        public static MeshedModel Load(string name, byte emission = 0)
        {
            List<VertexData[]> meshes = new List<VertexData[]>();

            int w = -1;
            int h = -1;

            foreach (string m in Directory.EnumerateFiles(ResourceManager.GetEntityPath(name)))
            {
                IVoxFile file = VoxReader.VoxReader.Read(m);
                IModel voxModel = file.Models[0];
                Model meshData = new Model();
                for (int i = 0; i < voxModel.Voxels.Length; i++)
                {
                    Voxel vox = voxModel.Voxels[i];
                    Color color = vox.Color;

                    meshData.SetBlock(vox.Position.X, vox.Position.Z, vox.Position.Y, Util.Color.ToInt(color.R, color.G, color.B));
                }

                meshes.Add(meshData.MeshSingle(emission));

                if (w == -1)
                {
                    w = meshData.Width;
                    h = meshData.Height;
                }
            }

            return new MeshedModel(w, h, meshes.ToArray());
        }
    }
}
