using OpenTK.Graphics.OpenGL;
using System;

namespace _3dTerrainGeneration.Engine.Graphics.Backend
{
    internal class OGLStateManager
    {
        private static int Program = -1, Framebuffer = -1;

        public static void UseProgram(int id)
        {
#if DEBUG
            if (id < 0)
            {
                throw new InvalidOperationException("Invalid shader program!");
            }
#endif
            if (Program != id)
            {
                Program = id;
                GL.UseProgram(id);
            }
        }

        public static void BindFrameBuffer(FramebufferTarget target, int id)
        {
            if (id != Framebuffer)
            {
                GL.BindFramebuffer(target, id);
                Framebuffer = id;
            } 
        }
    }
}
