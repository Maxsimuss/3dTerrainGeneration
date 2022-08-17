using OpenTK.Graphics.OpenGL;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

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
                Console.WriteLine($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
            }
        }

        protected static void LinkProgram(int program)
        {
            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                Console.WriteLine($"Error occurred whilst linking Program({program})");
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

        protected void Init()
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

        public void SetIntArr(string name, int[] data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data.Length, data);
        }

        public void SetFloat(string name, float data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data);
        }

        public unsafe void SetMatrix4(string name, Matrix4x4 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);

            GL.UniformMatrix4(_uniformLocations[name], 1, true, (float*)&data.M11);
        }

        public unsafe void SetMatrix4Arr(string name, Matrix4x4[] data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);

            fixed (Matrix4x4* ptr = data)
            {
                GL.UniformMatrix4(_uniformLocations[name], data.Length, true, (float*)ptr);
            }
        }

        public unsafe void SetVector2(string name, Vector2 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);
            GL.Uniform2(_uniformLocations[name], data.X, data.Y);
        }

        public unsafe void SetVector3(string name, Vector3 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);
            GL.Uniform3(_uniformLocations[name], data.X, data.Y, data.Z);
        }

        public unsafe void SetVector4(string name, Vector4 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return;

            GL.UseProgram(Handle);
            GL.Uniform4(_uniformLocations[name], data.X, data.Y, data.Z, data.W);
        }

        public void Dispose()
        {
            GL.DeleteProgram(Handle);
        }
    }
}
