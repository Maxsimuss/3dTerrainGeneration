using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.rendering
{
    internal class GLR
    {
        private static int Program, Framebuffer, ReadFramebuffer, DrawFramebuffer;

        public static void UseProgram(int id)
        {
            if(Program != id)
            {
                Program = id;
                GL.UseProgram(id);
            }
        }

        public static void BindFrameBuffer(FramebufferTarget target, int id)
        {
            switch (target)
            {
                case FramebufferTarget.ReadFramebuffer:
                    if(id != ReadFramebuffer)
                    {
                        GL.BindFramebuffer(target, id);
                        ReadFramebuffer = id;
                    }
                    break;
                case FramebufferTarget.DrawFramebuffer:
                    if (id != DrawFramebuffer)
                    {
                        GL.BindFramebuffer(target, id);
                        DrawFramebuffer = id;
                    }
                    break;
                case FramebufferTarget.Framebuffer:
                    if (id != Framebuffer)
                    {
                        GL.BindFramebuffer(target, id);
                        Framebuffer = id;
                    }
                    break;
                default:
                    throw new Exception("Target not supported.");
            }
        }
    }
}
