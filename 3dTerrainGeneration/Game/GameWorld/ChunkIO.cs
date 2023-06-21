#define NO_SAVE

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;


namespace _3dTerrainGeneration.Game.GameWorld
{
    internal class ChunkIO
    {
#if !NO_SAVE
        private static int version = 5;
#endif


        private static string GetChunkDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/3dterrain/maps/";
        }

        private static string GetChunkFile(int X, int Y, int Z)
        {
            return GetChunkDir() + X + "." + Y + "." + Z + ".ch";
        }

        public static void Save(Chunk chunk)
        {
#if !NO_SAVE

            Directory.CreateDirectory(GetChunkDir());
            string file = GetChunkFile(chunk.X, chunk.Y, chunk.Z);
            WriteStream stream = new WriteStream();

            stream.WriteInt(version);
            stream.WriteBool(chunk.empty);
            stream.WriteBool(chunk.full);

            if (!chunk.empty)
            {
                for (int i = 0; i < Chunk.lodCount; i++)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        stream.WriteArray(chunk.mesh[i][j]);
                    }
                }
                stream.WriteArray(chunk.blocks);
                stream.WriteArray(chunk.sounds.ToArray());
                stream.WriteArray(chunk.particles.ToArray());
            }

            stream.Save(file);

#endif
        }

        public static bool Load(Chunk chunk)
        {
#if NO_SAVE
            return false;
#else
            ReadStream stream = new ReadStream();
            string file = GetChunkFile(chunk.X, chunk.Y, chunk.Z);

            if (File.Exists(file))
            {
                stream.Load(file);

                if (stream.ReadInt() != version)
                {
                    return false;
                }

                chunk.empty = stream.ReadBool();
                chunk.full = stream.ReadBool();
                if (chunk.empty)
                {
                    return true;
                }


                uint[][][] mesh = new uint[Chunk.lodCount][][];
                for (int lod = 0; lod < Chunk.lodCount; lod++)
                {
                    mesh[lod] = new uint[6][];
                    for (int j = 0; j < 6; j++)
                    {
                        mesh[lod][j] = stream.ReadUIntArray();
                    }
                }
                chunk.mesh = mesh;
                chunk.blocks = stream.ReadByteArray();

                int soundCount = stream.ReadInt();
                for (int i = 0; i < soundCount; i += 4)
                {
                    Window.Instance.SoundManager.PlaySound(
                        (SoundType)stream.ReadByte(),
                        new Vector3(
                            stream.ReadByte() + chunk.X * Chunk.Size,
                            stream.ReadByte() + chunk.Y * Chunk.Size,
                            stream.ReadByte() + chunk.Z * Chunk.Size
                        ),
                        true
                    );
                }

                int particleCount = stream.ReadInt();
                for (int i = 0; i < particleCount; i += 4)
                {
                    Window.Instance.ParticleSystem.Emit(
                        stream.ReadByte() + chunk.X * Chunk.Size,
                        stream.ReadByte() + chunk.Y * Chunk.Size,
                        stream.ReadByte() + chunk.Z * Chunk.Size,
                        stream.ReadByte());
                }

                return true;
            }

            return false;
#endif
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
