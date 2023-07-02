using System.Collections.Generic;

namespace _3dTerrainGeneration.Engine.Graphics._3D.Particles
{
    internal class ParticleSystem
    {
        private static ParticleSystem instance;
        public static ParticleSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ParticleSystem();
                }

                return instance;
            }
        }

        private ParticleRenderer renderer;
        private List<ParticleEmmiter> emmiters;

        private ParticleSystem()
        {
            renderer = new ParticleRenderer();
            emmiters = new List<ParticleEmmiter>();
        }

        public ParticleEmmiter Emit(float x, float y, float z, float radius, float scale = .5f)
        {
            ParticleEmmiter emmiter = new ParticleEmmiter(renderer, new(x, y, z), radius, scale);

            emmiters.Add(emmiter);

            return emmiter;
        }

        public void RemoveEmmiter(ParticleEmmiter emmiter)
        {
            emmiters.Remove(emmiter);
        }

        public void Update(float dT)
        {
            renderer.Reset();

            foreach (var item in emmiters)
            {
                item.Update(dT);
            }
        }

        public void Render()
        {
            renderer.Render();
        }
    }
}
