using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Features;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using LibNoise.Modifier;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace _3dTerrainGeneration.Game.GameWorld.Generators
{
    internal enum BlockMask : uint
    {
        Fertile = 1,
        Structure = 1 << 1,
        Road = 1 << 2,
    }

    internal class TerrainGenerator
    {
        public BiomeGenerator BiomeGenerator;
        private List<IFeature> features;

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
            };
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

                    BiomeInfo biome = BiomeGenerator.GetBiomeInfo(X, Z);
                    bool isRoad = Math.Abs(NoiseUtil.GetPerlin(X, Z, 1000)) < .02f;

                    float height = NoiseUtil.OctavePerlinNoise(X, Z, 7, .5f, 3, 2000) * 100;
                    float variance = 1;

                    if (height < -25)
                    {
                        height += 25;
                        height *= 25;
                        variance *= 25;
                        height -= 25;
                    }

                    if (height < 0)
                    {
                        height /= 5;
                    }

                    if (height < -150)
                    {
                        height += 150;
                        height /= 5;
                        variance /= 5;
                        height -= 150;
                    }

                    if (height < -200)
                    {
                        height += 200;
                        height /= 5;
                        variance /= 5;
                        height -= 200;
                    }

                    if (height > 25)
                    {
                        height -= 25;
                        height *= 5;
                        variance *= 5;
                        height += 25;
                    }

                    if (height > 150)
                    {
                        height -= 150;
                        height /= 5;
                        variance /= 5;
                        height += 150;
                    }

                    if (height > -25 && height < 100)
                    {
                        height += 25;
                        height /= 125;
                        height *= 2;
                        height -= 1;
                        height = MathF.Pow(height, 3);
                        height += 1;
                        height /= 2;
                        height *= 125;
                        height -= 25;
                    }
                    else
                    {
                        isRoad = false;
                    }

                    height = (int)Math.Round(height);


                    for (int y = 0; y < Chunk.CHUNK_SIZE && y <= height - location.Y; y++)
                    {
                        int Y = location.Y + y;

                        if (height == Y)
                        {
                            uint block = 0;

                            if (NoiseUtil.GetPerlin(X, Z, 2) * .5 + .5 > biome.Temperature / -30)
                            {
                                if (isRoad)
                                {
                                    block = Color.ToInt(120, 80, 60);
                                    block |= (uint)BlockMask.Road;
                                }
                                else
                                {
                                    block = BiomeGenerator.GetGrassColor(biome);
                                    if (variance < 5)
                                    {
                                        block |= (uint)BlockMask.Fertile;
                                    }
                                }
                            }
                            else
                            {
                                block = Color.ToInt(200, 200, 220);
                            }


                            octree.SetVoxel(x, y, z, block);
                        }
                        else
                        {
                            octree.SetVoxel(x, y, z, Color.ToInt(100, 100, 100));
                        }
                    }
                }
            }

            chunk.HasTerrain = true;
        }

        public void Populate(Chunk chunk, ConcurrentDictionary<Vector3I, Chunk> chunks)
        {
            VoxelOctree octree = chunk.Blocks;
            Vector3I location = new Vector3I(chunk.X, chunk.Y, chunk.Z) * Chunk.CHUNK_SIZE;
            HashSet<Chunk> modifiedChunks = new HashSet<Chunk>();

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    int X = x + location.X;
                    int Z = z + location.Z;
                    BiomeInfo biome = BiomeGenerator.GetBiomeInfo(X, Z);

                    foreach (IFeature feature in features)
                    {
                        if (!feature.Inhabitable(biome)) continue;

                        for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                        {
                            feature.Process(chunk, chunks, modifiedChunks, x, y, z, biome, octree);
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

        public float Random(Vector3I pos)
        {
            return MathF.Abs(MathF.Sin(pos.X + (pos.Y + pos.Z * Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE));
        }

        public void PlaceStructure(Chunk chunk, ConcurrentDictionary<Vector3I, Chunk> chunks, HashSet<Chunk> modifiedChunks, Structure structure, Vector3I offset)
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

                    targetChunk.Blocks.SetVoxel(pos.X, pos.Y, pos.Z, item.Value | (uint)BlockMask.Structure);

                    if (targetChunk.IsPopulated)
                        modifiedChunks.Add(targetChunk);
                }
                else
                {
                    chunk.Blocks.SetVoxel(pos.X, pos.Y, pos.Z, item.Value | (uint)BlockMask.Structure);
                }
            }
        }
    }
}
