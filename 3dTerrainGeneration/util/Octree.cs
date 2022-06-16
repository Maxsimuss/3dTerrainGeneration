using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.util
{
    class Octree
    {
        byte val = 0;

        Octree[] nodes = null;

        public void setValue(byte x, byte y, byte z, byte val)
        {
            if (this.val == val) return;

            if(nodes == null)
            {
                nodes = new Octree[8];
            }

        }
    }
}
