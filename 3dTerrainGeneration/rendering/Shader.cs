using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.rendering
{
    public class Shader
    {
        public int Handle;
        private Dictionary<string, int> _uniformLocations;

        public Shader()
        {
            Handle = GL.CreateProgram();

        }

        protected static string LoadSource(string path)
        {
            using (var sr = new StreamReader(path, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        protected static void CompileShader(int shader)
        {
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                var infoLog = GL.GetShaderInfoLog(shader);
                //throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
            }
        }

        protected static void LinkProgram(int program)
        {
            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                //throw new Exception($"Error occurred whilst linking Program({program})");
            }
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }

        public void Init()
        {
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            _uniformLocations = new Dictionary<string, int>();

            for (var i = 0; i < numberOfUniforms; i++)
            {
                var key = GL.GetActiveUniform(Handle, i, out _, out _);
                var location = GL.GetUniformLocation(Handle, key);

                _uniformLocations.Add(key, location);
            }
        }

        public void SetInt(string name, int data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data);
        }

        public void SetFloat(string name, float data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data);
        }

        public void SetMatrix4(string name, Matrix4 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);
            GL.UniformMatrix4(_uniformLocations[name], true, ref data);
        }

        public void SetVector3(string name, Vector3 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);
            GL.Uniform3(_uniformLocations[name], data);
        }

        public void SetVector2(string name, Vector2 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);
            GL.Uniform2(_uniformLocations[name], data);
        }

        public void SetVector4(string name, Vector4 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);
            GL.Uniform4(_uniformLocations[name], data);
        }

        public void Dispose()
        {
            GL.DeleteProgram(Handle);
        }
    }
}
