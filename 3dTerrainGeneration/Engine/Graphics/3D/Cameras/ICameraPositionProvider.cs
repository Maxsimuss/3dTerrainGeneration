using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Graphics._3D.Cameras
{
    internal interface ICameraPositionProvider
    {
        public void Provide(Camera camera);
    }
}
