using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    class BiomeData
    {
        public float mountainess, mountainSharpness, baseHeight;

        public BiomeData()
        {

        }

        public BiomeData(float mountainess, float mountainSharpness, float baseHeight)
        {
            this.mountainess = mountainess;
            this.mountainSharpness = mountainSharpness;
            this.baseHeight = baseHeight;
        }

        public float Distance(float mountainess, float mountainSharpness, float baseHeight)
        {
            float dM = this.mountainess - mountainess;
            float dS = this.mountainSharpness - mountainSharpness;
            float dH = this.baseHeight - baseHeight;

            return (float)Math.Sqrt(dM * dM + dS * dS + dH * dH);
        }
    }
}
