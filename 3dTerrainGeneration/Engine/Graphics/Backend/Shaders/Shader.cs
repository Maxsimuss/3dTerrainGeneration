using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Shaders
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
            string basePath = System.Reflection.Assembly.GetAssembly(typeof(Shader)).Location;
            string[] pathArr = basePath.Split("\\");
            basePath = string.Join("\\", pathArr.Take(pathArr.Length - 1));

            string source;
            using (var sr = new StreamReader(basePath + "\\" + path, Encoding.UTF8))
            {
                source = sr.ReadToEnd();
            }

            string flags =
                """

                """;

            //flags = "";

            List<string> lines = source.Split("\n").ToList();
            lines.Insert(1, flags);

            return string.Join("\n", lines);
        }

        protected static void CompileShader(int shader)
        {
            GL.CompileShader(shader);
            //GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            //if (code != (int)All.True)
            //{
            //    var infoLog = GL.GetShaderInfoLog(shader);
            //    Console.WriteLine($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
            //}

            string log = GL.GetShaderInfoLog(shader);
            if (log.Length > 0)
            {
                Console.WriteLine(log);
                //throw new Exception(log);
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

            string log = GL.GetProgramInfoLog(program);
            if (log.Length > 0)
            {
                Console.WriteLine(log);
                //throw new Exception(log);
            }
        }

        public void Use()
        {
            OGLStateManager.UseProgram(Handle);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }

        protected void Init(bool a = true)
        {
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            _uniformLocations = new Dictionary<string, int>();

            if (a)
            {
                for (var i = 0; i < numberOfUniforms; i++)
                {
                    var key = GL.GetActiveUniform(Handle, i, out _, out _);
                    var location = GL.GetUniformLocation(Handle, key);

                    _uniformLocations.Add(key, location);
                }
            }
        }

        public Shader SetInt(string name, int data)
        {
            if (!_uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data);

            return this;
        }

        public Shader SetIntArr(string name, int[] data)
        {
            if (!_uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data.Length, data);
            return this;
        }

        public Shader SetFloatArr(string name, float[] data)
        {
            if (!_uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data.Length, data);
            return this;
        }

        public Shader SetFloat(string name, float data)
        {
            if (!_uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform1(_uniformLocations[name], data);
            return this;
        }

        public unsafe Shader SetMatrix4(string name, Matrix4x4 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);

            GL.UniformMatrix4(_uniformLocations[name], 1, true, &data.M11);
            return this;
        }

        public unsafe Shader SetMatrix4Arr(string name, Matrix4x4[] data)
        {
            if (!_uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);

            fixed (Matrix4x4* ptr = data)
            {
                GL.UniformMatrix4(_uniformLocations[name], data.Length, true, (float*)ptr);
            }
            return this;
        }

        public unsafe Shader SetVector2(string name, Vector2 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform2(_uniformLocations[name], data.X, data.Y);

            return this;
        }

        public unsafe Shader SetVector3(string name, Vector3 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform3(_uniformLocations[name], data.X, data.Y, data.Z);
            return this;
        }

        public unsafe Shader SetVector4(string name, Vector4 data)
        {
            if (!_uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform4(_uniformLocations[name], data.X, data.Y, data.Z, data.W);
            return this;
        }

        public void Dispose()
        {
            GL.DeleteProgram(Handle);

            Handle = -1;
        }
    }
}
