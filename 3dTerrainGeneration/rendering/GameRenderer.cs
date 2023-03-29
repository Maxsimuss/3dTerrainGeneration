//#define INTEL

using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace _3dTerrainGeneration.rendering
{
    public class InderectDraw
    {
        public int memStart, memEnd, first, count, instanceCount;
    }

    struct DrawArraysIndirectCommand
    {
        public uint count, instanceCount, first, baseInstance;
    }

    public class GameRenderer
    {
        private static GameRenderer instance = null;
        public static GameRenderer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameRenderer();
                }

                return instance;
            }
        }

        private readonly int matrixCount = 4096;
        private readonly int vertexSize = 1;

        private int VAO, MeshVBO, MatrixVBO, inderectBuffer;
        private List<InderectDraw> memory = new List<InderectDraw>();
        private List<Matrix4x4> matrices = new List<Matrix4x4>();
        private Queue<InderectDraw> queue = new Queue<InderectDraw>();

        public int VramUsage = 0;
        public readonly int VramAllocated = 1048576 * 384; //256 MB

        public GameRenderer()
        {
            VAO = GL.GenVertexArray();
            MeshVBO = GL.GenBuffer();
            MatrixVBO = GL.GenBuffer();
            inderectBuffer = GL.GenBuffer();

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, MeshVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, VramAllocated, IntPtr.Zero, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribIPointer(0, vertexSize, VertexAttribIntegerType.UnsignedInt, 0, IntPtr.Zero);
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

        public InderectDraw SubmitMesh(uint[] mesh, InderectDraw old = null)
        {
            memory.Remove(old);

            int end = 0;
            int index = memory.Count;
            int size = mesh.Length * sizeof(uint);
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
            if (old == null)
                draw = new InderectDraw();

            draw.memStart = end;
            draw.memEnd = end + size;
            draw.first = end / vertexSize / sizeof(uint);
            draw.count = size / vertexSize / sizeof(uint);

            memory.Insert(index, draw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, MeshVBO);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)draw.memStart, size, mesh);

            return draw;
        }

        public void FreeMemory(InderectDraw draw)
        {
            memory.Remove(draw);
        }

        public void QueueRender(InderectDraw draw, Matrix4x4 matrix)
        {
            if (draw.instanceCount == 0)
            {
                queue.Enqueue(draw);
            }

            draw.instanceCount++;
            matrices.Add(matrix);
        }

        public void Draw(FragmentShader shader)
        {
            GL.BindVertexArray(VAO);
            shader.Use();

            DrawArraysIndirectCommand[] inderect = new DrawArraysIndirectCommand[queue.Count];

            uint baseInst = 0;
            long vertDrawn = 0;

            for (int i = 0; i < inderect.Length; i++)
            {
                InderectDraw draw = queue.Dequeue();

                DrawArraysIndirectCommand cmd;
                cmd.first = (uint)draw.first;
                cmd.count = (uint)draw.count;

                cmd.baseInstance = baseInst;
                cmd.instanceCount = (uint)draw.instanceCount;

                vertDrawn += draw.count * draw.instanceCount;

                inderect[i] = cmd;

                baseInst += (uint)draw.instanceCount;
                draw.instanceCount = 0;
            }
            //Console.WriteLine("vert c {0}k", vertDrawn / 3000);
            GL.BindBuffer(BufferTarget.ArrayBuffer, MatrixVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, 64 * matrices.Count, matrices.ToArray(), BufferUsageHint.DynamicDraw);
            matrices.Clear();

            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, inderectBuffer);
            GL.BufferData(BufferTarget.DrawIndirectBuffer, inderect.Length * sizeof(uint) * 4, inderect, BufferUsageHint.DynamicDraw);

            if (memory.Count > 0)
            {
                InderectDraw d = memory[memory.Count - 1];
                VramUsage = d.first * sizeof(uint) * vertexSize + d.count * vertexSize * sizeof(uint);
            }

#if INTEL
            for (int i = 0; i < inderect.Length; i++)
            {
                DrawArraysIndirectCommand cmd = inderect[i];
                if(cmd.instanceCount > 0) {
                    GL.DrawArraysInstancedBaseInstance(PrimitiveType.Triangles, (int)cmd.first, (int)cmd.count, (int)cmd.instanceCount, (int)cmd.baseInstance);
                }
            }
#else
            GL.MultiDrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero, inderect.Length, 0);
#endif
        }
    }
}
