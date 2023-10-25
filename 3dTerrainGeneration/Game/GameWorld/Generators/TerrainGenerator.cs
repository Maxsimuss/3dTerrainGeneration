﻿using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Features;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace _3dTerrainGeneration.Game.GameWorld.Generators
{
    internal enum BlockMask : uint
    {
        Fertile = 1,
        Structure = 1 << 1,
        Road = 1 << 2,
        Important = 1 << 3,
    }

    internal class TerrainGenerator
    {
        public BiomeGenerator BiomeGenerator;
        private List<IFeature> features;

        [ThreadStatic]
        private static uint[] tempRow = new uint[Chunk.CHUNK_SIZE];

        public TerrainGenerator()
        {
            BiomeGenerator = new BiomeGenerator();

            features = new List<IFeature>
            {
                new RainbowTreeFeature(this),
                new PlainTreeFeature(this),
                new RockFeature(this),
                new CactusFeature(this),
                new PineFeature(this),
                new DeadTreeFeature(this),
                new CrystalFeature(this),
                new DeadBushFeature(this),
            };
        }



        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private extern static int GenerateTerrain(IntPtr svo, IntPtr biomeGenerator, int chunkSize, int locationX, int locationY, int locationZ);

        public void GenerateTerrain(Chunk chunk)
        {
            Vector3I location = new Vector3I(chunk.X, chunk.Y, chunk.Z) * Chunk.CHUNK_SIZE;

            int placedBlocks = GenerateTerrain(chunk.Blocks.Handle, BiomeGenerator.Handle, Chunk.CHUNK_SIZE, location.X, location.Y, location.Z);
            chunk.State |= ChunkState.HasTerrain;

            if (placedBlocks == 0)
            {
                chunk.State |= ChunkState.IsPopulated;
                chunk.Blocks.CompressEmpty();
            }

            if (placedBlocks == Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE)
            {
                chunk.State |= ChunkState.IsPopulated;
                //chunk.State |= ChunkState.NeedsRemeshing;
                chunk.Blocks.CompressEmpty();
                //chunk.Blocks.Compress();
            }
        }

        public void Populate(Chunk chunk, ChunkManager chunkManager)
        {
            Vector3I location = new Vector3I(chunk.X, chunk.Y, chunk.Z) * Chunk.CHUNK_SIZE;

            Stopwatch sw = Stopwatch.StartNew();
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    int X = x + location.X;
                    int Z = z + location.Z;
                    BiomeInfo biome = BiomeGenerator.GetBiomeInfo(X, Z);

                    chunk.Blocks.GetRow(x, z, Chunk.CHUNK_SIZE, tempRow);
                    foreach (IFeature feature in features)
                    {
                        if (!feature.Inhabitable(biome)) continue;


                        for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                        {
                            feature.Process(chunk, chunkManager, x, y, z, biome, tempRow);
                        }
                    }
                }
            }
            Console.WriteLine(sw.ElapsedMilliseconds);

            chunk.State |= ChunkState.IsPopulated;
            chunk.State |= ChunkState.NeedsRemeshing;
            //ChunkIO.Save(chunk);
        }

        public float Random(Vector3I pos)
        {
            return MathF.Abs(MathF.Sin(pos.X + (pos.Y + pos.Z * Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE));
        }

        public void PlaceStructure(Chunk chunk, ChunkManager chunkManager, Structure structure, Vector3I offset)
        {
            foreach (var item in structure.Data)
            {
                Vector3I pos = item.Key + offset;
                if (pos.X < 0 || pos.Y < 0 || pos.Z < 0 || pos.X >= Chunk.CHUNK_SIZE || pos.Y >= Chunk.CHUNK_SIZE || pos.Z >= Chunk.CHUNK_SIZE)
                {
                    Vector3I chunkLocation = new Vector3I(
                        (int)Math.Floor((float)pos.X / Chunk.CHUNK_SIZE) + chunk.X,
                        (int)Math.Floor((float)pos.Y / Chunk.CHUNK_SIZE) + chunk.Y,
                        (int)Math.Floor((float)pos.Z / Chunk.CHUNK_SIZE) + chunk.Z);

                    Chunk targetChunk = chunkManager.GetChunkAt(chunkLocation);

                    if (targetChunk == null || (targetChunk.State & ChunkState.HasTerrain) == 0)
                    {
                        chunk.State &= ~ChunkState.IsPopulated;

                        //stop population immediately, has overhead, but idc rn
                        throw new InvalidOperationException();
                    }

                    pos.X = MathUtil.Mod(pos.X, Chunk.CHUNK_SIZE);
                    pos.Y = MathUtil.Mod(pos.Y, Chunk.CHUNK_SIZE);
                    pos.Z = MathUtil.Mod(pos.Z, Chunk.CHUNK_SIZE);

                    targetChunk.Blocks.SetVoxel(pos.X, pos.Y, pos.Z, item.Value | (uint)BlockMask.Structure);
                    targetChunk.State |= ChunkState.NeedsRemeshing;
                }
                else
                {
                    chunk.Blocks.SetVoxel(pos.X, pos.Y, pos.Z, item.Value | (uint)BlockMask.Structure);
                }
            }
        }
    }
}
