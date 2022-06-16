using _3dTerrainGeneration.entity;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network;

namespace _3dTerrainGeneration.rendering
{
    public class ModelInstance
    {
        static int Limit = 500;

        Vector4[] transforms;
        int index;
        int vertexCount;
        int instanceVBO, modelVBO, VAO;

        public ModelInstance(ushort[] mesh)
        {
            instanceVBO = GL.GenBuffer();
            modelVBO = GL.GenBuffer();

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, modelVBO);
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.UnsignedShort, false, 4 * sizeof(ushort), 0);
            GL.BufferData(BufferTarget.ArrayBuffer, mesh.Length * sizeof(ushort), mesh, BufferUsageHint.StaticDraw);
            vertexCount = mesh.Length / 4;

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

            transforms = new Vector4[Limit * 4];
        }

        public void Add(Matrix4 matrix)
        {
            if (index >= Limit) return;

            transforms[index * 4] = matrix.Column0;
            transforms[index * 4 + 1] = matrix.Column1;
            transforms[index * 4 + 2] = matrix.Column2;
            transforms[index * 4 + 3] = matrix.Column3;
            index++;
        }

        public void Render()
        {
            if (index == 0) return;

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, index * 64, transforms, BufferUsageHint.DynamicDraw);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, modelVBO);
            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, vertexCount, index);

            index = 0;
        }
    }
}
