using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.rendering
{
    internal struct Mesh
    {
        public int Width, Height;
        public ushort[][] Data;

        public Mesh(int width, int height, ushort[][] data) : this()
        {
            Width = width;
            Height = height;
            Data = data;
        }
    }
}
