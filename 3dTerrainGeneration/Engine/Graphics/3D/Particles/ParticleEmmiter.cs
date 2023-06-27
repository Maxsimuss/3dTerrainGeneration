using System;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Graphics._3D.Particles
{
    internal class ParticleEmmiter
    {
        struct Particle
        {
            public Vector3 position;
            public float scale, rotationX, rotationY;
            public float TTL;
        }

        private static readonly Matrix4x4 ParticleOffset = Matrix4x4.CreateTranslation(-.5f, -.5f, -.5f);
        private static readonly int ParticleCount = 100;

        private ParticleRenderer renderer;
        private Random random;

        private Particle[] particles = new Particle[ParticleCount];
        private float time;

        public Vector3 Position;
        public float Radius;
        public float Scale;

        public ParticleEmmiter(ParticleRenderer renderer, Vector3 position, float radius, float scale)
        {
            Position = position;
            Radius = radius;
            Scale = scale;

            this.renderer = renderer;
            random = new Random();

            for (int i = 0; i < ParticleCount; i++)
            {
                Particle particle = new Particle();

                particle.TTL = i * 20f / ParticleCount;

                particles[i] = particle;
            }
        }


        public void Update(float dT)
        {
            time = (time + dT / 10) % (float)(Math.PI * 2);

            Vector3 velocity = new Vector3((float)Math.Sin(time) * .5f, 1.5f, (float)Math.Sin(time + 1) * .25f);

            for (int i = 0; i < ParticleCount; i++)
            {
                Particle particle = particles[i];

                if (particle.TTL <= 0)
                {
                    particle.TTL = random.NextSingle() * 10 + 10;
                    particle.rotationX = (float)(random.NextSingle() * Math.PI * 2);
                    particle.rotationY = (float)(random.NextSingle() * Math.PI * 2);
                    particle.scale = random.NextSingle() * .5f + .5f;
                    particle.position = Position +
                        new Vector3(random.NextSingle() - .5f, random.NextSingle() - .5f, random.NextSingle() - .5f)
                        * Radius;
                }

                particle.position -= velocity * dT / particle.scale / 2;
                particle.rotationX -= dT;
                particle.rotationY -= dT;
                particle.TTL -= dT;

                particles[i] = particle;

                renderer.Add(ParticleOffset
                    * Matrix4x4.CreateScale(particle.scale * Scale)
                    * Matrix4x4.CreateRotationX(particle.rotationX)
                    * Matrix4x4.CreateRotationY(particle.rotationY)
                    * Matrix4x4.CreateTranslation(particle.position));
            }
        }
    }
}
