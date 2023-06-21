using System.Runtime.InteropServices;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexData
    {
        public static int Size = sizeof(byte) * 8;

        private byte x, y, z, r, g, b, normal, reserved;

        public VertexData(int x, int y, int z, int normal, int r, int g, int b)
        {
            this.x = (byte)x;
            this.y = (byte)y;
            this.z = (byte)z;
            this.normal = (byte)normal;
            this.r = (byte)r;
            this.g = (byte)g;
            this.b = (byte)b;
            reserved = 0;
        }
    }
}
