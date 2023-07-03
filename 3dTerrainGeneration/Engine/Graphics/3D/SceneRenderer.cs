//#define INTEL

using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Graphics.Backend.Shaders;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Graphics._3D
{
    internal class InderectDraw
    {
        public int memStart, memEnd, first, count, instanceCount;
    }

    struct DrawArraysIndirectCommand
    {
        public uint count, instanceCount, first, baseInstance;
    }

    struct MeshSubmit
    {
        public int memStart, size;
        public VertexData[] mesh;
    }

    internal class SceneRenderer
    {
        private static SceneRenderer instance = null;
        public static SceneRenderer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SceneRenderer();
                }

                return instance;
            }
        }

        private readonly int matrixCount = 4096;

        private int VAO, MeshVBO, MatrixVBO, inderectBuffer;
        private List<InderectDraw> memory = new List<InderectDraw>();
        private List<Matrix4x4> matrices = new List<Matrix4x4>();
        private Queue<InderectDraw> queue = new Queue<InderectDraw>();
        private Queue<MeshSubmit> submitQueue = new Queue<MeshSubmit>();

        public int VramUsage = 0;
        public readonly int VramAllocated = 1048576 * 512; // 512MB

        private SceneRenderer()
        {
            VAO = GL.GenVertexArray();
            MeshVBO = GL.GenBuffer();
            MatrixVBO = GL.GenBuffer();
            inderectBuffer = GL.GenBuffer();

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, MeshVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, VramAllocated, IntPtr.Zero, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.UnsignedByte, false, 8, 0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.UnsignedByte, true, 8, 3);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.UnsignedByte, false, 8, 6);
            GL.VertexAttribDivisor(0, 0);
            GL.VertexAttribDivisor(1, 0);
            GL.VertexAttribDivisor(2, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, MatrixVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, 64 * matrixCount, IntPtr.Zero, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(3);
            GL.EnableVertexAttribArray(4);
            GL.EnableVertexAttribArray(5);
            GL.EnableVertexAttribArray(6);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 64, 0);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 64, 16);
            GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, 64, 32);
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, 64, 48);
            GL.VertexAttribDivisor(3, 1);
            GL.VertexAttribDivisor(4, 1);
            GL.VertexAttribDivisor(5, 1);
            GL.VertexAttribDivisor(6, 1);
        }

        public InderectDraw SubmitMesh(VertexData[] mesh, InderectDraw old = null)
        {
            memory.Remove(old);

            int end = 0;
            int index = memory.Count;
            int size = mesh.Length * VertexData.Size;
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
            draw.first = end / VertexData.Size;
            draw.count = size / VertexData.Size;

            memory.Insert(index, draw);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, MeshVBO);
            //GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)draw.memStart, size, mesh);

            submitQueue.Enqueue(new MeshSubmit() { memStart = draw.memStart, size = size, mesh = mesh });

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

        public void Render(FragmentShader shader)
        {
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, MeshVBO);

            while (submitQueue.Count > 0)
            {
                var draw = submitQueue.Dequeue();
                GL.BufferSubData(BufferTarget.ArrayBuffer, draw.memStart, draw.size, draw.mesh);
            }

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

            GL.BindBuffer(BufferTarget.ArrayBuffer, MatrixVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, 64 * matrices.Count, matrices.ToArray(), BufferUsageHint.DynamicDraw);
            matrices.Clear();

            if (memory.Count > 0)
            {
                InderectDraw d = memory[memory.Count - 1];
                VramUsage = d.first * VertexData.Size + d.count * VertexData.Size;
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
            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, inderectBuffer);
            GL.BufferData(BufferTarget.DrawIndirectBuffer, inderect.Length * sizeof(uint) * 4, inderect, BufferUsageHint.DynamicDraw);

            GL.MultiDrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero, inderect.Length, 0);
#endif
        }

        public void EnsureNotNull()
        {

        }
    }
}
