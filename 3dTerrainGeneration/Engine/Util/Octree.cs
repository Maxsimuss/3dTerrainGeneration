namespace _3dTerrainGeneration.Engine.Util
{
    public class VoxelOctree
    {
        //[DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        //private static extern IntPtr CreateSVO(int depth);

        //[DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        //private static extern void DeleteSVO(IntPtr svo);

        //[DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        //private static extern void SetVoxel(IntPtr svo, int x, int y, int z, uint value);

        //[DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        //private static extern uint GetValue(IntPtr svo, int x, int y, int z);

        //IntPtr svo;
        //public VoxelOctree(int depth)
        //{
        //    svo = CreateSVO(depth);
        //}

        //~VoxelOctree()
        //{
        //    DeleteSVO(svo);
        //}

        //public void SetVoxel(int x, int y, int z, uint value)
        //{
        //    SetVoxel(svo, x, y, z, value);
        //}

        //public uint GetValue(int x, int y, int z)
        //{
        //    return GetValue(svo, x, y, z);
        //}

        private OctreeNode root;

        public VoxelOctree(int depth)
        {
            root = new OctreeNode(0, 0, 0, (byte)depth);
        }

        public void SetVoxel(int x, int y, int z, uint value)
        {
            //lock (root)
            root.SetVoxel(x, y, z, value);
        }

        public uint GetValue(int x, int y, int z)
        {
            //lock (root)
            return root.GetValue(x, y, z);
        }
    }

    public struct OctreeNode
    {
        private bool isLeaf;
        private OctreeNode[] children;
        private uint value;

        private byte CenterX, CenterY, CenterZ;
        private byte depth;
        public OctreeNode(byte x, byte y, byte z, byte depth)
        {
            this.depth = depth;

            isLeaf = true;
            value = 0;

            byte childDepth = (byte)(depth - 1);
            byte childSize = (byte)(1 << childDepth);

            CenterX = (byte)(x + childSize);
            CenterY = (byte)(y + childSize);
            CenterZ = (byte)(z + childSize);
        }

        private void InitChildren()
        {
            byte childDepth = (byte)(depth - 1);
            byte childSize = (byte)(1 << childDepth);

            children = new OctreeNode[8];

            byte x = (byte)(CenterX - childSize);
            byte y = (byte)(CenterY - childSize);
            byte z = (byte)(CenterZ - childSize);

            children[0] = new OctreeNode(x, y, z, childDepth);
            children[1] = new OctreeNode((byte)(x + childSize), y, z, childDepth);
            children[2] = new OctreeNode(x, (byte)(y + childSize), z, childDepth);
            children[3] = new OctreeNode((byte)(x + childSize), (byte)(y + childSize), z, childDepth);
            children[4] = new OctreeNode(x, y, (byte)(z + childSize), childDepth);
            children[5] = new OctreeNode((byte)(x + childSize), y, (byte)(z + childSize), childDepth);
            children[6] = new OctreeNode(x, (byte)(y + childSize), (byte)(z + childSize), childDepth);
            children[7] = new OctreeNode((byte)(x + childSize), (byte)(y + childSize), (byte)(z + childSize), childDepth);
            isLeaf = false;
        }

        public void SetVoxel(int x, int y, int z, uint value)
        {
            if (depth == 0)
            {
                this.value = value;
            }
            else
            {
                if (isLeaf && this.value == value)
                {
                    return;
                }

                if (isLeaf)
                {
                    InitChildren();
                    for (int i = 0; i < 8; i++)
                    {
                        children[i].value = this.value;
                    }
                }

                int childIndex = GetChildIndex(x, y, z);
                children[childIndex].SetVoxel(x, y, z, value);

                TryCollapse();
            }
        }

        public uint GetValue(int x, int y, int z)
        {
            if (isLeaf)
            {
                return value;
            }
            else
            {
                int childIndex = GetChildIndex(x, y, z);
                return children[childIndex].GetValue(x, y, z);
            }
        }

        private int GetChildIndex(int x, int y, int z)
        {
            int childIndex = 0;
            if (x >= CenterX) childIndex |= 1;
            if (y >= CenterY) childIndex |= 2;
            if (z >= CenterZ) childIndex |= 4;
            return childIndex;
        }

        private void TryCollapse()
        {
            if (isLeaf) return;

            uint val = children[0].value;

            for (int i = 0; i < 8; i++)
            {
                if (!children[i].isLeaf || val != children[i].value)
                {
                    return;
                }
            }

            isLeaf = true;
            children = null;
            value = val;
        }
    }
}
