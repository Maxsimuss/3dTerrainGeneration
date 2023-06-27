using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System;
using System.Collections.Generic;

namespace _3dTerrainGeneration.Game.GameWorld.Generators
{
    internal class TerrainGenerator
    {
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
            Vector3I location = new Vector3I(chunk.X, chunk.Y, chunk.Z) * Chunk.CHUNK_SIZE;

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                int X = location.X + x;
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    int Z = location.Z + z;

                    BiomeInfo biome = biomeGenerator.GetBiomeInfo(X, Z);

                    int height = (int)Math.Round(NoiseUtil.OctavePerlinNoise(X, Z, 7, .5f, 2, 1000) * 50);

                    for (int y = 0; y < height - location.Y; y++)
                    {
                        octree.SetVoxel(x, y, z, Color.ToInt(100, 100, 100));
                    }

                    if (height - location.Y >= 0 && height - location.Y < Chunk.CHUNK_SIZE)
                    {
                        octree.SetVoxel(x, height - location.Y, z, biomeGenerator.GetGrassColor(biome));
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

        public void Populate(Chunk chunk, Dictionary<Vector3I, Chunk> chunks)
        {
            VoxelOctree octree = chunk.Blocks;
            Vector3I location = new Vector3I(chunk.X, chunk.Y, chunk.Z) * Chunk.CHUNK_SIZE;
            HashSet<Chunk> modifiedChunks = new HashSet<Chunk>();

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                    {
                        int X = location.X + x;
                        int Y = location.Y + y;
                        int Z = location.Z + z;

                        if (y < Chunk.CHUNK_SIZE - 1 && octree.GetValue(x, y, z) != 0 && octree.GetValue(x, y + 1, z) == 0 && NoiseUtil.GetPerlin(X, Z, 2) > .98f)
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
            return MathF.Abs(MathF.Sin(pos.X + (pos.Y + pos.Z * Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE));
        }

        private void PlaceStructure(Chunk chunk, Dictionary<Vector3I, Chunk> chunks, HashSet<Chunk> modifiedChunks, Structure structure, Vector3I offset)
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

                    Chunk targetChunk;
                    if (chunks.ContainsKey(chunkLocation))
                    {
                        targetChunk = chunks[chunkLocation];
                    }
                    else
                    {
                        targetChunk = new Chunk(chunk.World, chunkLocation.X, chunkLocation.Y, chunkLocation.Z);
                        chunks[chunkLocation] = targetChunk;
                    }

                    if (!targetChunk.HasTerrain)
                    {
                        GenerateTerrain(targetChunk);
                    }

                    pos.X = MathUtil.Mod(pos.X, Chunk.CHUNK_SIZE);
                    pos.Y = MathUtil.Mod(pos.Y, Chunk.CHUNK_SIZE);
                    pos.Z = MathUtil.Mod(pos.Z, Chunk.CHUNK_SIZE);

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
