using _3dTerrainGeneration.Engine.Util;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Shaders
{
    public class Shader : IDisposable
    {
        private bool ready = false;
        private Dictionary<string, int> uniformLocations = new Dictionary<string, int>();
        private string flags = "#pragma optionNV(strict on)\n"; // #pragma optionNV(unroll all) #pragma optionNV(inline all)
        protected int Handle { get; private set; } = -1;

        public Shader()
        {
            Handle = GL.CreateProgram();
        }

        public Shader Define(string name, float value)
        {
            flags += "#define " + name + " " + value;

            return this;
        }

        public virtual Shader Compile()
        {
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);
            for (var i = 0; i < numberOfUniforms; i++)
            {
                var key = GL.GetActiveUniform(Handle, i, out _, out _);
                var location = GL.GetUniformLocation(Handle, key);

                uniformLocations.Add(key, location);
            }

            ready = true;

            return this;
        }

        protected string LoadSource(string path)
        {
            string source = ResourceManager.GetShaderSource(path);
            List<string> lines = source.Split("\n").ToList();
            lines.Insert(1, flags);

            return string.Join("\n", lines);
        }

        protected static void CompileShader(int shader)
        {
            GL.CompileShader(shader);

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

            string log = GL.GetProgramInfoLog(program);
            if (log.Length > 0)
            {
                Console.WriteLine(log);
                //throw new Exception(log);
            }
        }

        public void Use()
        {
            if (!ready)
            {
                throw new Exception("Shader not compiled!");
            }

            OGLStateManager.UseProgram(Handle);
        }

        public Shader SetInt(string name, int data)
        {
            if (!uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform1(uniformLocations[name], data);

            return this;
        }

        public Shader SetIntArr(string name, int[] data)
        {
            if (!uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform1(uniformLocations[name], data.Length, data);
            return this;
        }

        public Shader SetFloatArr(string name, float[] data)
        {
            if (!uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform1(uniformLocations[name], data.Length, data);
            return this;
        }

        public Shader SetFloat(string name, float data)
        {
            if (!uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform1(uniformLocations[name], data);
            return this;
        }

        public unsafe Shader SetMatrix4(string name, Matrix4x4 data)
        {
            if (!uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);

            GL.UniformMatrix4(uniformLocations[name], 1, true, &data.M11);
            return this;
        }

        public unsafe Shader SetMatrix4Arr(string name, Matrix4x4[] data)
        {
            if (!uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);

            fixed (Matrix4x4* ptr = data)
            {
                GL.UniformMatrix4(uniformLocations[name], data.Length, true, (float*)ptr);
            }
            return this;
        }

        public unsafe Shader SetVector2(string name, Vector2 data)
        {
            if (!uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform2(uniformLocations[name], data.X, data.Y);

            return this;
        }

        public unsafe Shader SetVector3(string name, Vector3 data)
        {
            if (!uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform3(uniformLocations[name], data.X, data.Y, data.Z);
            return this;
        }

        public unsafe Shader SetVector4(string name, Vector4 data)
        {
            if (!uniformLocations.ContainsKey(name)) return this;

            OGLStateManager.UseProgram(Handle);
            GL.Uniform4(uniformLocations[name], data.X, data.Y, data.Z, data.W);
            return this;
        }

        public void Dispose()
        {
            GL.DeleteProgram(Handle);

            Handle = -1;
        }
    }
}
