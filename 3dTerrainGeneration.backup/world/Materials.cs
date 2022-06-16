using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    class Materials
    {
        public Dictionary<byte, Material> materials = new Dictionary<byte, Material>();
     
        public Materials()
        {
            materials.Add(MaterialType.STONE, new Material(new OpenTK.Vector3(102, 96, 89)));
            materials.Add(MaterialType.GRASS, new Material(new OpenTK.Vector3(72, 130, 60)));
            materials.Add(MaterialType.SNOW, new Material(new OpenTK.Vector3(203, 238, 245)));
            materials.Add(MaterialType.WOOD, new Material(new OpenTK.Vector3(102, 57, 22)));
            materials.Add(MaterialType.PINE_LEAVES1, new Material(new OpenTK.Vector3(76, 148, 64)));
            materials.Add(MaterialType.PINE_LEAVES2, new Material(new OpenTK.Vector3(91, 181, 76)));
            materials.Add(MaterialType.SAKURA_LEAVES1, new Material(new OpenTK.Vector3(191, 137, 214)));
            materials.Add(MaterialType.SAKURA_LEAVES2, new Material(new OpenTK.Vector3(212, 109, 214)));
            materials.Add(MaterialType.MAPLE_LEAVES1, new Material(new OpenTK.Vector3(242, 92, 73)));
            materials.Add(MaterialType.MAPLE_LEAVES2, new Material(new OpenTK.Vector3(222, 102, 47)));
        }
    }

    public static class MaterialType
    {
        public static byte AIR = 0;
        public static byte STONE = 1;
        public static byte GRASS = 2;
        public static byte SNOW = 3;
        public static byte WOOD = 4;
        public static byte PINE_LEAVES1 = 5;
        public static byte PINE_LEAVES2 = 6;
        public static byte SAKURA_LEAVES1 = 7;
        public static byte SAKURA_LEAVES2 = 8;
        public static byte MAPLE_LEAVES1 = 9;
        public static byte MAPLE_LEAVES2 = 10;
    }
}
