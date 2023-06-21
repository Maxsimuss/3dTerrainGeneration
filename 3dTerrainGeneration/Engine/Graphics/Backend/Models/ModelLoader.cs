using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Chunks;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Color = _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Color;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models
{
    internal class ModelLoader
    {
        static string ReadChars(byte[] data, ref int i, int c)
        {
            char[] chars = new char[c];
            Encoding.ASCII.GetDecoder().GetChars(data, i, c, chars, 0, true);
            i += c;
            return new string(chars);
        }

        static int ReadInt(byte[] data, ref int i)
        {
            i += 4;
            return BitConverter.ToInt32(data, i);
        }

        public static MeshedModel Load(string name, byte emission = 0)
        {
            List<VertexData[]> meshes = new List<VertexData[]>();

            int w = -1;
            int h = -1;

            foreach (string m in Directory.EnumerateFiles("Resources/models/" + name))
            {
                IVoxFile file = VoxReader.VoxReader.Read(m);

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

                Model meshData = new Model();
                for (int i = 0; i < voxelChunk.Voxels.Length; i++)
                {
                    RawVoxel vox = voxelChunk.Voxels[i];
                    Color color = palleteChunk.Colors[vox.ColorIndex - 1];

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
