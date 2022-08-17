using _3dTerrainGeneration.util;
using _3dTerrainGeneration.world;
using OpenTK.Graphics.OpenGL;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.rendering
{
    internal class ParticleRenderer
    {
        private static int MaxParticles = 100000;
        int index;
        Matrix4x4[] transforms;
        int instanceVBO, cubeVBO, VAO;

        public ParticleRenderer()
        {
            instanceVBO = GL.GenBuffer();
            cubeVBO = GL.GenBuffer();

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            MeshData meshData = new MeshData();
            meshData.SetBlock(0, 0, 0, Color.ToInt(86, 217, 50));
            ushort[] mesh = meshData.MeshSingle(0);

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, cubeVBO);
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.UnsignedShort, false, 4 * sizeof(ushort), 0);
            GL.BufferData(BufferTarget.ArrayBuffer, mesh.Length * sizeof(ushort), mesh, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);
            GL.EnableVertexAttribArray(4);
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVBO);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 64, 0);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 64, 16);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 64, 32);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 64, 48);

            GL.VertexAttribDivisor(0, 0);
            GL.VertexAttribDivisor(1, 1);
            GL.VertexAttribDivisor(2, 1);
            GL.VertexAttribDivisor(3, 1);
            GL.VertexAttribDivisor(4, 1);

            transforms = new Matrix4x4[MaxParticles];
        }
        public void Add(Matrix4x4 matrix)
        {
            if (index >= MaxParticles) return;

            transforms[index] = matrix;
            index++;
        }

        public void Render()
        {
            if (index == 0) return;

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, index * 64, transforms, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, cubeVBO);
            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 36, index);
        }

        public void Reset()
        {
            index = 0;
        }
    }
}
