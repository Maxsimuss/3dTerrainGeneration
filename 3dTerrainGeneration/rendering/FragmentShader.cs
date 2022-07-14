using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace _3dTerrainGeneration.rendering
{
    public class FragmentShader : Shader
    {
        public FragmentShader(string vertPath, string fragPath) : base()
        {
            string vertexSource = LoadSource(vertPath);
            string fragmentSource = LoadSource(fragPath);

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            
            GL.ShaderSource(vertexShader, vertexSource);
            GL.ShaderSource(fragmentShader, fragmentSource);

            CompileShader(vertexShader);
            CompileShader(fragmentShader);

            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);

            LinkProgram(Handle);

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            Init();
        }
    }
}
