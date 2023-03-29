using _3dTerrainGeneration.util;
using _3dTerrainGeneration.world;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace _3dTerrainGeneration.rendering
{
    public class Farlands
    {
        InderectDraw farlands;

        public Farlands()
        {
            Generate();
        }

        public void Generate()
        {
            //float[,] heightmap = new float[128, 128];
            //uint[,] colormap = new uint[128, 128];
            //for (byte x = 0; x < 128; x++)
            //{
            //    int X = x * 32 - 2048;
            //    for (byte z = 0; z < 128; z++)
            //    {
            //        int Z = z * 32 - 2048;

            //        float h = (float)Math.Clamp(Math.Pow(Chunk.OcataveNoise(X, Z, .0005f / 4, 8) * 1.2, 7) * Chunk.GetPerlin(X, Z, .0005f / 4) * 255 / 2 - Chunk.Size / 16, 0, 255);
            //        heightmap[x, z] = h;
            //        float temp = Chunk.smoothstep(0, 1, Chunk.GetPerlin(X, Z, .00025f) - (h * 8 / 512f));
            //        float humidity = Chunk.smoothstep(0, 1, Chunk.GetPerlin(X + 12312, Z - 124124, .00025f));

            //        if (temp < .16)
            //        {
            //            colormap[x, z] = Color.ToInt(255, 255, 255);
            //        }
            //        else
            //        {
            //            temp -= .16f;
            //            temp /= 1f - .16f;
            //            colormap[x, z] = Color.HsvToRgb(
            //                    150 - (byte)((byte)(temp * 8) * 13),
            //                    166 + (byte)((byte)(humidity * 4) * 16),
            //                    220 - (byte)((byte)(humidity * 4) * 15)
            //            );
            //        }
            //    }
            //}
            //List<uint> quad = new List<uint>();
            //for (byte x = 0; x < 128 - 1; x++)
            //{
            //    for (byte z = 0; z < 128 - 1; z++)
            //    {
            //        byte h0 = (byte)heightmap[x, z];
            //        byte h1 = (byte)heightmap[x + 1, z];
            //        byte h2 = (byte)heightmap[x + 1, z + 1];
            //        byte h3 = (byte)heightmap[x, z + 1];

            //        uint c0 = colormap[x, z];
            //        uint c1 = colormap[x + 1, z];
            //        uint c2 = colormap[x + 1, z + 1];
            //        uint c3 = colormap[x, z + 1];

            //        Vector3 a = new Vector3(x, h0, z);
            //        Vector3 b = new Vector3(x + 1, h1, z);
            //        Vector3 c = new Vector3(x + 1, h2, z + 1);
            //        Vector3 normal = (-Vector3.Cross(b - a, c - a) + Vector3.One) * 7;
            //        AddPoint((byte)normal.X, (byte)normal.Y, (byte)normal.Z, (byte)(x + 1), h2, (byte)(z + 1), c2);
            //        AddPoint((byte)normal.X, (byte)normal.Y, (byte)normal.Z, (byte)(x + 1), h1, z, c1);
            //        AddPoint((byte)normal.X, (byte)normal.Y, (byte)normal.Z, x, h0, z, c0);

            //        AddPoint((byte)normal.X, (byte)normal.Y, (byte)normal.Z, x, h3, (byte)(z + 1), c3);
            //        AddPoint((byte)normal.X, (byte)normal.Y, (byte)normal.Z, (byte)(x + 1), h2, (byte)(z + 1), c2);
            //        AddPoint((byte)normal.X, (byte)normal.Y, (byte)normal.Z, x, h0, z, c0);
            //    }
            //}

            //void AddPoint(byte nx, byte ny, byte nz, byte x, byte y, byte z, uint color)
            //{
            //    byte r = (byte)(color >> 16 & 0xFF);
            //    byte g = (byte)(color >> 8 & 0xFF);
            //    byte b = (byte)(color & 0xFF);

            //    quad.Add((ushort)(x | y << 8));
            //    quad.Add((ushort)(z | r << 8));
            //    quad.Add((ushort)(g | b << 8));
            //    quad.Add((ushort)(0 | nx << 4 | ny << 8 | nz << 12));
            //}
            //farlands = World.gameRenderer.SubmitMesh(quad.ToArray(), farlands);
        }

        public void Render()
        {
            //World.gameRenderer.QueueRender(farlands, Matrix4x4.CreateTranslation(-64, 0, -64) * Matrix4x4.CreateScale(32, 8, 32));
        }
    }
}
