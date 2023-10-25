using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Engine.World;
using _3dTerrainGeneration.Game.GameWorld.Entities;
using System;
using System.Numerics;
using static OpenTK.Mathematics.MathHelper;

namespace _3dTerrainGeneration.Game.GameWorld
{
    internal class World : IWorld
    {
        private static readonly float SunPitch = DegreesToRadians(25);

        public double Time = 1000 * 420;
        public Vector3 SunPosition { get; set; }

        private ChunkManager chunkManager;

        private Random rnd = new Random();


        public World()
        {
            chunkManager = new ChunkManager(this);
        }

        public bool IsChunkLoadedAt(double x, double y, double z)
        {
            double chunkX = x / Chunk.CHUNK_SIZE;
            double chunkY = y / Chunk.CHUNK_SIZE;
            double chunkZ = z / Chunk.CHUNK_SIZE;

            return chunkManager.IsChunkLoadedAt(new Vector3I((int)Math.Floor(chunkX), (int)Math.Floor(chunkY), (int)Math.Floor(chunkZ)));
        }

        public Vector3I PickSpawnLocation(Vector3I origin, Func<uint, bool> blockCondition)
        {
            Vector3I position = new Vector3I(rnd.Next(-256, 256), 0, rnd.Next(-256, 256)) + origin;
            for (int i = 0; i < 256; i++)
            {
                if (!IsChunkLoadedAt(position.X, position.Y + i, position.Z) || !IsChunkLoadedAt(position.X, position.Y + i + 1, position.Z))
                {
                    continue;
                }

                if (blockCondition(GetBlockAt(position.X, position.Y + i, position.Z)) && GetBlockAt(position.X, position.Y + i + 1, position.Z) == 0)
                {
                    position.Y += i + 1;
                    return position;
                }

                if (!IsChunkLoadedAt(position.X, position.Y - i, position.Z) || !IsChunkLoadedAt(position.X, position.Y - i + 1, position.Z))
                {
                    continue;
                }

                if (blockCondition(GetBlockAt(position.X, position.Y - i, position.Z)) && GetBlockAt(position.X, position.Y - i + 1, position.Z) == 0)
                {
                    position.Y += -i + 1;
                    return position;
                }
            }

            throw new InvalidOperationException("Unable to find a suitable spawn location!");
        }

        public void Tick(Vector3 origin)
        {
            Vector3I originCoord = new Vector3I((int)origin.X, (int)origin.Y, (int)origin.Z);
            Vector3I originChunkCoord = originCoord / Chunk.CHUNK_SIZE;

            //try
            //{
            //    Vector3I position = PickSpawnLocation(originCoord, b => b != 0);
            //    Frog entity = new Frog(this, EntityManager.Instance.GetNextEntityId());
            //    entity.Position = new Vector3(position.X, position.Y, position.Z);
            //    EntityManager.Instance.AddEntity(entity);
            //}
            //catch (InvalidOperationException e)
            //{

            //}
        }

        public uint GetBlockAt(double x, double y, double z)
        {
            double chunkX = x / Chunk.CHUNK_SIZE;
            double chunkY = y / Chunk.CHUNK_SIZE;
            double chunkZ = z / Chunk.CHUNK_SIZE;

            Vector3I chunkCoord = new Vector3I((int)Math.Floor(chunkX), (int)Math.Floor(chunkY), (int)Math.Floor(chunkZ));
            if (!chunkManager.IsChunkLoadedAt(chunkCoord)) return (uint)(y < 0 ? 1 : 0); // throw new Exception("Chunk not loaded!");

            Chunk chunk = chunkManager.GetChunkAt(chunkCoord);

            x %= Chunk.CHUNK_SIZE;
            y %= Chunk.CHUNK_SIZE;
            z %= Chunk.CHUNK_SIZE;

            if (x < 0)
            {
                x += Chunk.CHUNK_SIZE;
            }

            if (y < 0)
            {
                y += Chunk.CHUNK_SIZE;
            }

            if (z < 0)
            {
                z += Chunk.CHUNK_SIZE;
            }

            return chunk.GetBlockAt((int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z));
        }

        public int Render(Camera camera)
        {
            //Time += fT * 5000;
            //Time += fT * 20000;
            Time = 800000;
            float t = (float)(Time / 1000 / 1440 % 1);

            double X = MathF.Cos(t * 2 * MathF.PI - MathF.PI * .5f) * MathF.Cos(SunPitch);
            double Y = MathF.Sin(t * 2 * MathF.PI - MathF.PI * .5f) * MathF.Cos(SunPitch);
            double Z = MathF.Sin(SunPitch);

            SunPosition = new Vector3((float)X, (float)Y, (float)Z);

            return RenderWorld(camera.Position, camera.GetViewMatrix() * camera.GetProjectionMatrix(), false);
        }

        public int RenderWorld(Vector3 origin, Matrix4x4 mat, bool ortho, Vector3 viewDirection = default)
        {
            chunkManager.RenderChunks(origin, mat, ortho, viewDirection);

            return 0;
        }
    }
}
