using System.Collections.Generic;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Shaders
{
    internal class ShaderManager
    {
#if DEBUG
        private static readonly string path = "../../../shaders/";
#else
        private static readonly string path = "shaders/";
#endif

        private static ShaderManager instance;
        public static ShaderManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ShaderManager();
                }

                return instance;
            }
        }

        private Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();

        public FragmentShader RegisterFragmentShader(string name, string vertexPath, string fragmentPath)
        {
            if (shaders.ContainsKey(name))
            {
                shaders[name].Dispose();
            }

            FragmentShader shader = new FragmentShader(path + vertexPath, path + fragmentPath);
            InsertShader(shader, name);

            return shader;
        }

        public ComputeShader RegisterComputeShader(string name, string fragmentPath)
        {
            if (shaders.ContainsKey(name))
            {
                shaders[name].Dispose();
            }

            ComputeShader shader = new ComputeShader(path + fragmentPath);
            InsertShader(shader, name);

            return shader;
        }

        private void InsertShader(Shader shader, string name)
        {
            shaders[name] = shader;
        }

        public Shader this[string name]
        {
            get
            {
                return shaders[name];
            }
        }
    }
}
