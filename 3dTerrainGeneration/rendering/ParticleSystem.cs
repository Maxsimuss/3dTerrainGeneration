using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.rendering
{
    internal class ParticleSystem
    {
        private ParticleRenderer renderer;
        private List<ParticleEmmiter> emmiters;
        private object emmiterLock = new object();

        public ParticleSystem()
        {
            renderer = new ParticleRenderer();
            emmiters = new List<ParticleEmmiter>();
        }

        public ParticleEmmiter Emit(float x, float y, float z, float radius)
        {
            ParticleEmmiter emmiter = new ParticleEmmiter(renderer, new(x, y, z), radius, .5f);

            lock (emmiterLock)
                emmiters.Add(emmiter);

            return emmiter;
        }

        public void RemoveEmmiter(ParticleEmmiter emmiter)
        {
            lock (emmiterLock)
                emmiters.Remove(emmiter);
        }

        public void Update(Camera camera, float dT)
        {
            renderer.Reset();
            lock (emmiterLock)
                foreach (var item in emmiters)
                {
                    Vector3 diff = item.Position - camera.Position;
                    if (diff.Length < 100)
                    {
                        double fr = Math.Cos(1.65806);

                        if (Vector3.Dot(camera.Front, diff.Normalized()) >= fr)
                        {
                            item.Update(dT);
                        }
                    }
                }
        }

        public void Render()
        {
            renderer.Render();
        }
    }
}
