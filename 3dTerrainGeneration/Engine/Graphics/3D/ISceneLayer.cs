using _3dTerrainGeneration.Engine.Graphics._3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Graphics._3D
{
    internal interface ISceneLayer
    {
        public void Render(Camera camera, Matrix4x4 matrix);
    }
}
