using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxReader;

namespace _3dTerrainGeneration.rendering
{
    internal class MeshLoader
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

        public static Mesh Load(string name, byte emission = 0)
        {
            List<uint[]> meshes = new List<uint[]>();

            int w = -1;
            int h = -1;

            foreach (string m in Directory.EnumerateFiles("Resources/models/" + name))
            {
                VoxReader.Interfaces.IVoxFile file = VoxReader.VoxReader.Read(m);

                VoxReader.Chunks.VoxelChunk voxelChunk = null;
                VoxReader.Chunks.PaletteChunk palleteChunk = null;
                for (int i = 0; i < file.Chunks.Length; i++)
                {
                    if(file.Chunks[i].Type == ChunkType.Voxel)
                    {
                        voxelChunk = (VoxReader.Chunks.VoxelChunk)file.Chunks[i];
                    }
                    if (file.Chunks[i].Type == ChunkType.Palette)
                    {
                        palleteChunk = (VoxReader.Chunks.PaletteChunk)file.Chunks[i];
                    }
                }

                MeshData meshData = new MeshData();
                for (int i = 0; i < voxelChunk.Voxels.Length; i++)
                {
                    RawVoxel vox = voxelChunk.Voxels[i];
                    Color color = palleteChunk.Colors[vox.ColorIndex - 1];

                    meshData.SetBlock(vox.Position.X, vox.Position.Z, vox.Position.Y, 
                        util.Color.ToInt(color.R, color.G, color.B));
                }

                meshes.Add(meshData.MeshSingle(emission));

                if(w == -1)
                {
                    w = meshData.Width;
                    h = meshData.Height;
                }
            }

            return new Mesh(w, h, meshes.ToArray());
        }
    }
}
