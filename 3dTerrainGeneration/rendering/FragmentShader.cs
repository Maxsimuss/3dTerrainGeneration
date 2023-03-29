using OpenTK.Graphics.OpenGL;
using System;
using System.IO;

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

            if (!fragPath.Contains("taa.frag") || true)
            {
                GL.ShaderSource(vertexShader, vertexSource);
                CompileShader(vertexShader);
                GL.ShaderSource(fragmentShader, fragmentSource);
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
            else
            {
                byte[] bytes = File.ReadAllBytes("C:\\Users\\maksi\\source\\repos\\3dTerrainGeneration\\3dTerrainGeneration\\shaders\\post\\taa.spv");
                GL.ShaderBinary(1, ref fragmentShader, (BinaryFormat)All.ShaderBinaryFormatSpirV, bytes, bytes.Length);
                GL.SpecializeShader(fragmentShader, "main", 0, (int[])null, (int[])null);
                bytes = File.ReadAllBytes("C:\\Users\\maksi\\source\\repos\\3dTerrainGeneration\\3dTerrainGeneration\\shaders\\post.spv");
                GL.ShaderBinary(1, ref vertexShader, (BinaryFormat)All.ShaderBinaryFormatSpirV, bytes, bytes.Length);
                GL.SpecializeShader(vertexShader, "main", 0, (int[])null, (int[])null);

                GL.AttachShader(Handle, vertexShader);
                GL.AttachShader(Handle, fragmentShader);

                LinkProgram(Handle);

                GL.DetachShader(Handle, vertexShader);
                GL.DetachShader(Handle, fragmentShader);
                GL.DeleteShader(fragmentShader);
                GL.DeleteShader(vertexShader);
                Init(false);
            }
        }
    }
}
