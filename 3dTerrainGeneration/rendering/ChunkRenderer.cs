using _3dTerrainGeneration.world;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.rendering
{
    public class InderectDraw
    {
        public int memStart, memEnd, first, count;
        public Matrix4 matrix;
        public bool draw;
    }

    struct DrawArraysIndirectCommand
    {
        public uint count, instanceCount, first, baseInstance;
    }

    public class ChunkRenderer
    {
        private readonly int alloc = 1073741824, matrixCount = 4096;

        private int VAO, MeshVBO, MatrixVBO, inderectBuffer, memoryTop;
        private List<InderectDraw> memory = new List<InderectDraw>();
        
        public ChunkRenderer()
        {
            VAO = GL.GenVertexArray();
            MeshVBO = GL.GenBuffer();
            MatrixVBO = GL.GenBuffer();
            inderectBuffer = GL.GenBuffer();

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, MeshVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, alloc, IntPtr.Zero, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.UnsignedShort, false, 4 * sizeof(ushort), 0);
            GL.VertexAttribDivisor(0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, MatrixVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, 64 * matrixCount, IntPtr.Zero, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 64, 0);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 64, 16);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 64, 32);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 64, 48);
            GL.VertexAttribDivisor(1, 1);
            GL.VertexAttribDivisor(2, 1);
            GL.VertexAttribDivisor(3, 1);
            GL.VertexAttribDivisor(4, 1);
        }

        public InderectDraw SubmitMesh(ushort[] mesh, Matrix4 matrix, InderectDraw old)
        {
            memory.Remove(old);

            int end = 0;
            int index = memory.Count;
            int size = mesh.Length * sizeof(ushort);
            for (int i = 0; i < memory.Count; i++)
            {
                if (memory[i].memStart - end >= size)
                {
                    index = i;
                    break;
                }

                end = memory[i].memEnd;
            }

            InderectDraw draw = old;
            if(old == null)
                draw = new InderectDraw();

            draw.memStart = end;
            draw.memEnd = end + size;
            draw.first = end / 4 / sizeof(ushort);
            draw.count = size / 4 / sizeof(ushort);
            draw.matrix = matrix;

            memoryTop = Math.Max(memoryTop, draw.memEnd);
            Window.message = string.Format("{0}mb / {1}mb", memoryTop / 1024 / 1024, alloc / 1024 / 1024);

            memory.Insert(index, draw);
            GL.NamedBufferSubData(MeshVBO, (IntPtr)draw.memStart, size, mesh);

            return draw;
        }

        public void FreeMemory(InderectDraw draw)
        {
            memory.Remove(draw);
        }

        public void Draw(FragmentShader shader)
        {
            GL.BindVertexArray(VAO);
            shader.Use();

            Matrix4[] matrices = new Matrix4[memory.Count];
            DrawArraysIndirectCommand[] inderect = new DrawArraysIndirectCommand[memory.Count];
            for (int i = 0; i < inderect.Length; i++)
            {
                InderectDraw draw = memory[i];

                DrawArraysIndirectCommand cmd;
                cmd.first = (uint)draw.first;
                cmd.count = (uint)draw.count;
                cmd.baseInstance = (uint)i;
                cmd.instanceCount = (uint)(draw.draw ? 1 : 0);
                draw.draw = false;
                inderect[i] = cmd;
                matrices[i] = draw.matrix;
            }
            GL.NamedBufferData(MatrixVBO, 64 * matrices.Length, matrices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, inderectBuffer);
            GL.BufferData(BufferTarget.DrawIndirectBuffer, inderect.Length * sizeof(uint) * 4, inderect, BufferUsageHint.DynamicDraw);

            GL.MultiDrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero, inderect.Length, 0);
        }
    }
}
