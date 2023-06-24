using _3dTerrainGeneration.Engine.Graphics.Backend.Shaders;
using _3dTerrainGeneration.Engine.Graphics.UI.Screens;
using _3dTerrainGeneration.Engine.Input;
using _3dTerrainGeneration.Engine.Options;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Graphics.UI
{
    internal unsafe class UIRenderer
    {
        private static readonly int BUFFER_SIZE = 6 * 6 * 10000000;

        private static UIRenderer instance;
        public static UIRenderer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UIRenderer();
                }

                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        private int VAO, VBO;

        private float* vertexBuffer;
        private int index = 0, prev = 0;
        private IntPtr sync = IntPtr.Zero;

        private List<BaseScreen> openScreens = new List<BaseScreen>();
        Vector2 cursor;

        private UIRenderer()
        {
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            GL.BufferStorage(BufferTarget.ArrayBuffer, BUFFER_SIZE, IntPtr.Zero, BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapCoherentBit);
            vertexBuffer = (float*)GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, BUFFER_SIZE, BufferAccessMask.MapWriteBit | BufferAccessMask.MapPersistentBit | BufferAccessMask.MapCoherentBit);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 6 * sizeof(float), 2 * sizeof(float));
        }

        public void DrawRect(float x, float y, float x2, float y2, Vector4 color, bool flush = true)
        {
            if (index >= BUFFER_SIZE / 6)
            {
                Flush(ShaderManager.Instance["UI"]);
            }

            vertexBuffer[index++] = x;
            vertexBuffer[index++] = y;
            vertexBuffer[index++] = color.X;
            vertexBuffer[index++] = color.Y;
            vertexBuffer[index++] = color.Z;
            vertexBuffer[index++] = color.W;

            vertexBuffer[index++] = x2;
            vertexBuffer[index++] = y;
            vertexBuffer[index++] = color.X;
            vertexBuffer[index++] = color.Y;
            vertexBuffer[index++] = color.Z;
            vertexBuffer[index++] = color.W;

            vertexBuffer[index++] = x2;
            vertexBuffer[index++] = y2;
            vertexBuffer[index++] = color.X;
            vertexBuffer[index++] = color.Y;
            vertexBuffer[index++] = color.Z;
            vertexBuffer[index++] = color.W;

            vertexBuffer[index++] = x;
            vertexBuffer[index++] = y;
            vertexBuffer[index++] = color.X;
            vertexBuffer[index++] = color.Y;
            vertexBuffer[index++] = color.Z;
            vertexBuffer[index++] = color.W;

            vertexBuffer[index++] = x2;
            vertexBuffer[index++] = y2;
            vertexBuffer[index++] = color.X;
            vertexBuffer[index++] = color.Y;
            vertexBuffer[index++] = color.Z;
            vertexBuffer[index++] = color.W;

            vertexBuffer[index++] = x;
            vertexBuffer[index++] = y2;
            vertexBuffer[index++] = color.X;
            vertexBuffer[index++] = color.Y;
            vertexBuffer[index++] = color.Z;
            vertexBuffer[index++] = color.W;

            if (flush)
            {
                Flush(ShaderManager.Instance["UI"]);
            }
        }

        public void Flush(Shader shader = null)
        {
            //sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, 0);
            //GL.ClientWaitSync(sync, ClientWaitSyncFlags.SyncFlushCommandsBit, 1000000);
            //GL.DeleteSync(sync);

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            if (shader != null)
                shader.Use();
            else
                ShaderManager.Instance["UI"].Use();

            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, prev / 6, (index - prev) / 6);
            
            if (index >= BUFFER_SIZE / 4)
            {
                index = 0;
            }
            prev = index;

            //buffer.Clear();
        }

        public void Render()
        {
            foreach (var screen in openScreens)
            {
                screen.Render();
            }
        }

        public BaseScreen OpenScreen(BaseScreen screen)
        {
            openScreens.Add(screen);

            return screen;
        }
        public void CloseScreen(BaseScreen screen)
        {
            openScreens.Remove(screen);
        }

        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState)
        {
            float sens = (float)OptionManager.Instance["Controls", "Mouse Sensitivity"];
            cursor += new Vector2(mouseState.Delta.X * sens / 200, -mouseState.Delta.Y * sens / 200 * GraphicsEngine.Instance.AspectRatio);
            cursor.X = Math.Clamp(cursor.X, -1, 1);
            cursor.Y = Math.Clamp(cursor.Y, -1, 1);

            for (int i = openScreens.Count - 1; i >= 0; i--)
            {
                IScreenInputHandler handler = openScreens[i] as IScreenInputHandler;
                if (handler != null && handler.HandleInput(keyboardState, mouseState, cursor))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
