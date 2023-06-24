using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.RenderActions
{
    internal class EndProfilerSectionAction : IRenderAction
    {
        public void Apply()
        {
            GPUProfilter.Instance.EndSection();
        }
    }
}
