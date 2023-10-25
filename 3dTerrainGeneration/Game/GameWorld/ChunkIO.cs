using _3dTerrainGeneration.Engine.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace _3dTerrainGeneration.Game.GameWorld
{
    internal class ChunkIO
    {
        private static int version = 1;

        private static string GetChunkDir()
        {
            return ResourceManager.GetUserDataPath() + "/maps/";
        }

        private static string GetChunkFile(int X, int Y, int Z)
        {
            return GetChunkDir() + X + "." + Y + "." + Z + ".ch";
        }

        public static void Save(Chunk chunk)
        {
            Directory.CreateDirectory(GetChunkDir());
            string file = GetChunkFile(chunk.X, chunk.Y, chunk.Z);
            WriteStream stream = new WriteStream();

            stream.WriteInt(version);
            stream.WriteInt((int)chunk.State);

            uint[] blocks = new uint[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    uint[] row = new uint[Chunk.CHUNK_SIZE];
                    chunk.Blocks.GetRow(x, z, Chunk.CHUNK_SIZE, row);

                    Array.Copy(row, 0, blocks, (x * Chunk.CHUNK_SIZE + z) * Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE);
                }
            }

            stream.WriteArray(blocks);

            stream.Save(file);
        }

        public static bool Load(Chunk chunk)
        {
            ReadStream stream = new ReadStream();
            string file = GetChunkFile(chunk.X, chunk.Y, chunk.Z);

            if (File.Exists(file))
            {
                stream.Load(file);

                if (stream.ReadInt() != version)
                {
                    return false;
                }

                chunk.State = (ChunkState)stream.ReadInt() | ChunkState.NeedsRemeshing;

                uint[] blocks = stream.ReadUIntArray();

                for (int i = 0; i < blocks.Length; i++)
                {
                    int x = i / Chunk.CHUNK_SIZE / Chunk.CHUNK_SIZE;
                    int z = i / Chunk.CHUNK_SIZE % Chunk.CHUNK_SIZE;
                    int y = i % Chunk.CHUNK_SIZE;

                    chunk.Blocks.SetVoxel(x, y, z, blocks[i]);
                }


                return true;
            }

            return false;
        }
    }

    public class WriteStream
    {
        List<byte> data = new List<byte>();

        public void Save(string file)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data.ToArray(), 0, data.Count);
            }
            File.WriteAllBytes(file, output.ToArray());
        }

        public void WriteBool(bool val)
        {
            data.Add((byte)(val ? 1 : 0));
        }

        public void WriteInt(int val)
        {
            data.AddRange(BitConverter.GetBytes(val));
        }

        public void WriteArray(uint[] val)
        {
            WriteInt(val.Length);

            for (int i = 0; i < val.Length; i++)
            {
                data.AddRange(BitConverter.GetBytes(val[i]));
            }
        }

        public void WriteArray(byte[] val)
        {
            WriteInt(val.Length);

            data.AddRange(val);
        }
    }

    public class ReadStream
    {
        int offset = 0;
        byte[] data;

        public void Load(string file)
        {
            MemoryStream input = new MemoryStream(File.ReadAllBytes(file));
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            data = output.ToArray();
        }

        public int ReadInt()
        {
            return BitConverter.ToInt32(data, (offset += 4) - 4);
        }

        public uint ReadUIntShort()
        {
            return BitConverter.ToUInt32(data, (offset += 4) - 4);
        }

        public ushort ReadByte()
        {
            return data[offset++];
        }

        public bool ReadBool()
        {
            return data[offset++] == 1 ? true : false;
        }

        public uint[] ReadUIntArray()
        {
            int length = ReadInt();
            uint[] dat = new uint[length];

            for (int i = 0; i < length; i++)
            {
                dat[i] = ReadUIntShort();
            }

            return dat;
        }

        public byte[] ReadByteArray()
        {
            int length = ReadInt();
            byte[] dat = new byte[length];

            Array.Copy(data, offset, dat, 0, length);
            offset += length;

            return dat;
        }
    }
}
