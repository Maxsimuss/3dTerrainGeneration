using CSCore.XAudio2;
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace _3dTerrainGeneration.Engine.Util
{
    public unsafe struct NativeHybridOctree
    {
        public bool isCompressed;
        public int size;
        public NativeOctreeNode node;
        public uint* uncomressedData;
    }

    public unsafe struct NativeOctreeNode
    {
        public bool isLeaf;
        public uint value;
        public NativeOctreeNode* children;

        public byte centerX, centerY, centerZ;
        public byte depth;
    }

    public class VoxelOctree
    {
        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreateSVO(int depth);

        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern void DeleteSVO(IntPtr svo);

        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private extern static void GetRow(IntPtr svo, IntPtr data, int x, int z, int size);

        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint GetValue(IntPtr svo, int x, int y, int z);

        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetVoxel(IntPtr svo, int x, int y, int z, uint value);

        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern void FillArea(IntPtr svo, uint[] data, int x, int y, int z, int w, int h, int d);


        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern void Compress(IntPtr svo);

        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern void CompressEmpty(IntPtr svo);

        public IntPtr Handle { get; private set; }
        public VoxelOctree(int depth)
        {
            Handle = CreateSVO(depth);
        }

        ~VoxelOctree()
        {
            DeleteSVO(Handle);
        }

        public unsafe void SetVoxel(int x, int y, int z, uint value)
        {
            NativeHybridOctree octree = *((NativeHybridOctree*)Handle);
            if (octree.isCompressed)
            {
                SetVoxel(Handle, x, y, z, value);
            }
            else
            {
                octree.uncomressedData[(x * octree.size + z) * octree.size + y] = value;
            }
        }

        public void FillArea(uint[] data, int x, int y, int z, int w, int h, int d)
        {
            FillArea(Handle, data, x, y, z, w, h, d);
        }

        public unsafe uint GetValue(int x, int y, int z)
        {
            //either pinvoke overhead or clr overhead... :(

            //return GetValue(Handle, x, y, z);

            NativeHybridOctree* octree = ((NativeHybridOctree*)Handle);
            if (octree->isCompressed)
            {
                return GetValueCompressed(x, y, z, octree->node);
            }
            else
            {
                return GetValueUncompressed(x, y, z);
            }
        }

        private unsafe uint GetValueUncompressed(int x, int y, int z)
        {
            NativeHybridOctree* octree = ((NativeHybridOctree*)Handle);
            return octree->uncomressedData[(x * octree->size + z) * octree->size + y];
        }

        private unsafe uint GetValueCompressed(int x, int y, int z, NativeOctreeNode node)
        {
            if (node.isLeaf)
            {
                return node.value;
            }
            else
            {
                int childIndex = 0;
                if (x >= node.centerX) childIndex |= 1;
                if (y >= node.centerY) childIndex |= 2;
                if (z >= node.centerZ) childIndex |= 4;

                return GetValueCompressed(x, y, z, node.children[childIndex]);
            }
        }

        public unsafe void GetRow(int x, int z, int size, uint[] data)
        {
            fixed (uint* _data = data)
            {
                GetRow(Handle, (IntPtr)_data, x, z, size);
            }
        }

        public void Compress()
        {
            Compress(Handle);
        }

        public void CompressEmpty()
        {
            CompressEmpty(Handle);
        }

        //private OctreeNode root;

        //public VoxelOctree(int depth)
        //{
        //    root = new OctreeNode(0, 0, 0, (byte)depth);
        //}

        //public void SetVoxel(int x, int y, int z, uint value)
        //{
        //    //lock (root)
        //    root.SetVoxel(x, y, z, value);
        //}

        //public uint GetValue(int x, int y, int z)
        //{
        //    //lock (root)
        //    return root.GetValue(x, y, z);
        //}
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

            children = ArrayPool<OctreeNode>.Shared.Rent(8);

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
            ArrayPool<OctreeNode>.Shared.Return(children);
            //children = null;
            value = val;
        }
    }
}
