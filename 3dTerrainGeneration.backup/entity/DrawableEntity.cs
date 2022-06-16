using _3dTerrainGeneration.world;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.entity
{
    internal class DrawableEntity : EntityBase
    {
        public short[] mesh;
        private int Buffer;

        public DrawableEntity(World world, MeshGenerator meshGen, int Buffer) : base(world)
        {
            this.Buffer = Buffer;
            byte[][][] blocks = new byte[1][][];
            bool[][] usedBlocks = new bool[1][];
            
            for (int i = 0; i < 1; i++)
            {
                blocks[i] = new byte[1][];
                usedBlocks[i] = new bool[meshGen.materials.materials.Count];
                for (int j = 0; j < 1; j++)
                {
                    blocks[i][j] = new byte[1];
                    blocks[i][j][0] = MaterialType.MAPLE_LEAVES1;
                    usedBlocks[0][blocks[i][j][0] - 1] = true;
                }
            }

            mesh = meshGen.GenerateMeshFromBlocks(blocks, 1, 1, 1, usedBlocks);

            GL.BindVertexArray(Buffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, Buffer);
            GL.BufferData(BufferTarget.ArrayBuffer, 0, (IntPtr)0, BufferUsageHint.DynamicDraw);
            GL.BufferData(BufferTarget.ArrayBuffer, mesh.Length * sizeof(short), mesh, BufferUsageHint.DynamicDraw);
        }

        public void Render(Shader shader)
        {
            GL.BindVertexArray(Buffer);

            shader.SetMatrix4("model", Matrix4.Identity * Matrix4.CreateTranslation((float)(x - aabb.width), (float)y, (float)(z - aabb.width)));
            GL.DrawArrays(PrimitiveType.Quads, 0, mesh.Length);
        }
    }
}
