using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.rendering
{
    class DedParticles
    {
        class Particle
        {
            public Vector3 position, velocity;
            public float life;
        }

        private FragmentShader shader;

        private readonly int MaxParticles = 1638400;
        private int VAO, particleVBO, transformVBO;
        private int index;

        List<Particle> particles = new List<Particle>();
        private Vector3[] transforms;

        public DedParticles()
        {
            transforms = new Vector3[MaxParticles];

            transformVBO = GL.GenBuffer();
            particleVBO = GL.GenBuffer();

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            float[] particleMesh = new float[] {
                -1.0f,  1.0f,
                -1.0f, -1.0f,
                 1.0f, -1.0f,
                -1.0f,  1.0f,
                 1.0f, -1.0f,
                 1.0f,  1.0f
            };

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, particleVBO);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.BufferData(BufferTarget.ArrayBuffer, particleMesh.Length * sizeof(float), particleMesh, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, transformVBO);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            GL.VertexAttribDivisor(0, 0);
            GL.VertexAttribDivisor(1, 1);

            loadShader();
        }

        public void loadShader()
        {
            if (shader != null) shader.Dispose();

#if DEBUG
            string path = "../../../shaders/";
#else
            string path = "shaders/";
#endif

            shader = new FragmentShader(path + "particles.vert", path + "particles.frag");
        }

        public void Submit(Vector3 pos, Vector3 vel, float life)
        {
            Particle p = new();
            p.position = pos;
            p.velocity = vel;
            p.life = life;

            particles.Add(p);
        }

        public void Update(float time)
        {
            index = 0;

            int count = Math.Min(particles.Count, MaxParticles - 1);
            for (int i = 0; i < count; i++)
            {
                Particle p = particles[i];
                p.position += p.velocity * time;
                p.life -= time;

                transforms[i] = p.position;
            }
            index = count;

            particles.RemoveAll((p) => p.life <= 0);

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, transformVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, index * sizeof(float) * 3, transforms, BufferUsageHint.DynamicDraw);
        }

        public void Render(Matrix4x4 view, Matrix4x4 proj, float aspect)
        {
            GL.BindVertexArray(VAO);

            shader.Use();
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", proj);
            shader.SetFloat("aspect", aspect);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, index);
        }
    }
}
