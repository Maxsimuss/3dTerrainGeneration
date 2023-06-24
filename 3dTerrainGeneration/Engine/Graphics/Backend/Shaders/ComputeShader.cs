using OpenTK.Graphics.OpenGL;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Shaders
{
    public class ComputeShader : Shader
    {
        private string srcPath;

        public ComputeShader(string srcPath) : base()
        {
            this.srcPath = srcPath;
        }

        public override Shader Compile()
        {
            string source = LoadSource(srcPath);

            int shader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(shader, source);

            CompileShader(shader);
            GL.AttachShader(Handle, shader);

            LinkProgram(Handle);
            GL.DetachShader(Handle, shader);
            GL.DeleteShader(shader);

            return base.Compile();
        }
    }
}
