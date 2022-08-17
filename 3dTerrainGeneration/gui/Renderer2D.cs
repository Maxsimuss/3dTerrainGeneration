using _3dTerrainGeneration.rendering;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.gui
{
    public unsafe class Renderer2D
    {
        static int VAO, VBO;
        static FragmentShader shader;

        private static float* buffer;
        private static int index = 0, prev = 0, bufferSize = 6 * 4 * 10000000;
        private static IntPtr sync = IntPtr.Zero;

        public static void Init()
        {
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            GL.BufferStorage(BufferTarget.ArrayBuffer, bufferSize, IntPtr.Zero, BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapCoherentBit);
            buffer = (float*)GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, bufferSize, BufferAccessMask.MapWriteBit | BufferAccessMask.MapPersistentBit | BufferAccessMask.MapCoherentBit);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 6 * sizeof(float), 2 * sizeof(float));
        }

        public static void LoadShader(string path)
        {
            if(shader != null) shader.Dispose();
            shader = new FragmentShader(path + "rect.vert", path + "rect.frag");
        }

        public static void DrawRect(float x, float y, float x2, float y2, Vector4 color, bool flush = true)
        {
            if (index >= bufferSize / 4)
            {
                Flush(shader);
            }

            buffer[index++] = x;
            buffer[index++] = y;
            buffer[index++] = color.X;
            buffer[index++] = color.Y;
            buffer[index++] = color.Z;
            buffer[index++] = color.W;

            buffer[index++] = x2;
            buffer[index++] = y;
            buffer[index++] = color.X;
            buffer[index++] = color.Y;
            buffer[index++] = color.Z;
            buffer[index++] = color.W;

            buffer[index++] = x2;
            buffer[index++] = y2;
            buffer[index++] = color.X;
            buffer[index++] = color.Y;
            buffer[index++] = color.Z;
            buffer[index++] = color.W;

            buffer[index++] = x;
            buffer[index++] = y2;
            buffer[index++] = color.X;
            buffer[index++] = color.Y;
            buffer[index++] = color.Z;
            buffer[index++] = color.W;

            if (flush)
            {
                Flush(shader);
            }
        }

        public static void Flush(FragmentShader shader = null)
        {
            //sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, 0);
            //GL.ClientWaitSync(sync, ClientWaitSyncFlags.SyncFlushCommandsBit, 1000000);
            //GL.DeleteSync(sync);

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            if (shader != null)
                shader.Use();
            else
                Renderer2D.shader.Use();
            
            GL.DrawArrays(PrimitiveType.Quads, prev / 6, (index - prev) / 6);
            if (index >= bufferSize / 4)
            {
                index = 0;
            }
            prev = index;

            //buffer.Clear();
        }
    }
}
