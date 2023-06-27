using OpenTK.Graphics.OpenGL;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Shaders
{
    public class FragmentShader : Shader
    {
        private string vertPath, fragPath;

        public FragmentShader(string vertPath, string fragPath) : base()
        {
            this.vertPath = vertPath;
            this.fragPath = fragPath;
        }

        public override Shader Compile()
        {
            string vertexSource = LoadSource(vertPath);
            string fragmentSource = LoadSource(fragPath);

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(vertexShader, vertexSource);
            CompileShader(vertexShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            CompileShader(fragmentShader);

            //GL.ShaderBinary(1, ref fragmentShader, (BinaryFormat)All.ShaderBinaryFormatSpirV, bytes, bytes.Length);
            //GL.SpecializeShader(fragmentShader, "main", 0, null, (int[])null);

            //GL.ShaderBinary(1, ref vertexShader, (BinaryFormat)All.ShaderBinaryFormatSpirV, bytes, bytes.Length);
            //GL.SpecializeShader(vertexShader, "main", 0, null, (int[])null);

            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);

            LinkProgram(Handle);

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            return base.Compile();
        }
    }
}
