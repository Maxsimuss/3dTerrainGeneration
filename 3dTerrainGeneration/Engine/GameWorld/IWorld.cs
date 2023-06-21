using OpenTK.Graphics.ES20;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.World
{
    internal interface IWorld
    {
        public Vector3 SunPosition { get; }

        public void Tick(Vector3 origin);

        public uint GetBlockAt(double x, double y, double z);
    }
}
