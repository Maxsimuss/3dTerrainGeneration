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

        public void Emit(float x, float y, float z, float radius)
        {
            lock(emmiterLock)
                emmiters.Add(new ParticleEmmiter(renderer, new(x, y, z), radius, .5f));
        }

        public void Render(Camera camera, float dT)
        {

            lock(emmiterLock)
                foreach (var item in emmiters)
                {
                    Vector3 diff = item.Position - camera.Position;
                    if (diff.Length < 100)
                    {
                        double fr = Math.Cos(1.65806);

                        if(Vector3.Dot(camera.Front, diff.Normalized()) >= fr)
                        {
                            item.Render(dT);
                        }
                    }
                }

            renderer.Render();
        }
    }
}
