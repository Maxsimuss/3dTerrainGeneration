using _3dTerrainGeneration.util;
using LibNoise.Primitive;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using _3dTerrainGeneration.util;
using _3dTerrainGeneration.entity;
using OpenTK.Graphics.ES11;
using OpenTK.Graphics.OpenGL;

namespace _3dTerrainGeneration.world
{
    internal class TerrainGenerator
    {
        private static readonly int Size = GameSettings.CHUNK_SIZE;

        private BiomeGenerator biomeGenerator;
        private TreeGenerator treeGenerator;
        private List<Structure> structures;

        public TerrainGenerator()
        {
            biomeGenerator = new BiomeGenerator();
            treeGenerator = new TreeGenerator(1234);
            structures = new List<Structure>();

            for (int i = 0; i < 100; i++)
            {
                structures.Add(treeGenerator.GenerateBlobTree());
            }
        }

        public void GenerateTerrain(Chunk chunk)
        {
            VoxelOctree octree = chunk.Blocks;
            Vector3I location = new Vector3I(chunk.X, chunk.Y, chunk.Z) * Size;

            for (int x = 0; x < Size; x++)
            {
                int X = location.X + x;
                for (int z = 0; z < Size; z++)
                {
                    int Z = location.Z + z;

                    BiomeInfo biome = biomeGenerator.GetBiomeInfo(X, Z);

                    int height = (int)Math.Round(NoiseUtil.OctavePerlinNoise(X, Z, 7, .5f, 2, 1000) * 50);

                    for (int y = 0; y < height - location.Y; y++)
                    {
                        octree.SetVoxel(x, y, z, Materials.IdOf(100, 100, 100));
                    }

                    if (height - location.Y >= 0 && height - location.Y < Size)
                    {
                        octree.SetVoxel(x, height - location.Y, z, Materials.IdOf(biomeGenerator.GetGrassColor(biome)));
                    }

                    //for (int y = 0; y < Size; y++)
                    //{
                    //    int Y = location.Y + y;

                    //    if (value > Y)
                    //    {
                    //        if (value <= Y + 1)
                    //        {
                    //            octree.SetVoxel(x, y, z, Materials.IdOf(70, 180, 80));
                    //        }
                    //        else
                    //        {
                    //            octree.SetVoxel(x, y, z, Materials.IdOf(100, 100, 100));
                    //        }
                    //    }
                    //}
                }
            }

            chunk.HasTerrain = true;
        }

        public void Populate(Chunk chunk, ConcurrentDictionary<Vector3I, Chunk> chunks)
        {
            chunk.IsPopulated = true;
            return;

            VoxelOctree octree = chunk.Blocks;
            Vector3I location = new Vector3I(chunk.X, chunk.Y, chunk.Z) * Size;
            HashSet<Chunk> modifiedChunks = new HashSet<Chunk>();

            for (int x = 0; x < Size; x++)
            {
                for (int z = 0; z < Size; z++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        int X = location.X + x;
                        int Y = location.Y + y;
                        int Z = location.Z + z;

                        if (y < Size - 1 && octree.GetValue(x, y, z) == Materials.IdOf(70, 180, 80) && octree.GetValue(x, y + 1, z) == 0 && NoiseUtil.GetPerlin(X, Z, 2) > .98f)
                        {
                            Vector3I localPos = new Vector3I(x, y, z);
                            PlaceStructure(chunk, chunks, modifiedChunks, structures[(int)(Random(localPos) * (structures.Count - 1))], localPos);
                        }
                    }
                }
            }

            foreach (var item in modifiedChunks)
            {
                item.IsRemeshingNeeded = true;
            }
            chunk.IsPopulated = true;
        }

        private float Random(Vector3I pos)
        {
            return MathF.Abs(MathF.Sin(pos.X + (pos.Y + pos.Z * Chunk.Size) * Chunk.Size));
        }

        private void PlaceStructure(Chunk chunk, ConcurrentDictionary<Vector3I, Chunk> chunks, HashSet<Chunk> modifiedChunks, Structure structure, Vector3I offset)
        {
            foreach (var item in structure.Data)
            {
                Vector3I pos = item.Key + offset;
                if (pos.X < 0 || pos.Y < 0 || pos.Z < 0 || pos.X >= Chunk.Size || pos.Y >= Chunk.Size || pos.Z >= Chunk.Size)
                {
                    Vector3I chunkLocation = new Vector3I(
                        (int)Math.Floor((float)pos.X / Chunk.Size) + chunk.X,
                        (int)Math.Floor((float)pos.Y / Chunk.Size) + chunk.Y,
                        (int)Math.Floor((float)pos.Z / Chunk.Size) + chunk.Z);

                    Chunk targetChunk;
                    if (chunks.ContainsKey(chunkLocation))
                    {
                        targetChunk = chunks[chunkLocation];
                    }
                    else
                    {
                        targetChunk = new Chunk(chunk.world, chunkLocation.X, chunkLocation.Y, chunkLocation.Z);
                        chunks[chunkLocation] = targetChunk;
                    }

                    if (!targetChunk.HasTerrain)
                    {
                        GenerateTerrain(targetChunk);
                    }

                    pos.X = MathUtil.Mod(pos.X, Chunk.Size);
                    pos.Y = MathUtil.Mod(pos.Y, Chunk.Size);
                    pos.Z = MathUtil.Mod(pos.Z, Chunk.Size);

                    targetChunk.Blocks.SetVoxel(pos.X, pos.Y, pos.Z, item.Value);

                    if (targetChunk.IsPopulated)
                        modifiedChunks.Add(targetChunk);
                }
                else
                {
                    chunk.Blocks.SetVoxel(pos.X, pos.Y, pos.Z, item.Value);
                }
            }
        }
    }
}
