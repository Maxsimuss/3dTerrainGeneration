using OpenTK.Graphics.OpenGL4;

namespace _3dTerrainGeneration.rendering
{
    public class ComputeShader : Shader
    {
        public ComputeShader(string srcPath) : base()
        {
            string source = LoadSource(srcPath);

            int shader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(shader, source);

            CompileShader(shader);
            GL.AttachShader(Handle, shader);

            LinkProgram(Handle);
            GL.DetachShader(Handle, shader);
            GL.DeleteShader(shader);

            Init();
        }
    }
}
