using _3dTerrainGeneration.Game.GameWorld.Generators;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Game.GameWorld.Workers
{
    internal class TerrainGenerationWorker : IWorker<Chunk>
    {
        private ConcurrentQueue<Chunk> queue = new ConcurrentQueue<Chunk>();
        private TerrainGenerator generator;

        Action<Chunk> callback;
        public TerrainGenerationWorker(TerrainGenerator generator, Action<Chunk> callback = null)
        {
            this.generator = generator;
            this.callback = callback;

            RunTask();
        }

        private void RunTask()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (queue.Count == 0)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    if (queue.TryDequeue(out Chunk chunk))
                    {
                        generator.GenerateTerrain(chunk);
                        chunk.State |= ChunkState.NeedsRemeshing;

                        chunk.State &= ~ChunkState.AwaitingTerrainGeneration;

                        callback(chunk);
                    }
                }
            });
        }

        public void SubmitWork(Chunk chunk)
        {
            chunk.State |= ChunkState.AwaitingTerrainGeneration;
            queue.Enqueue(chunk);
        }

        public bool IsBusy()
        {
            return queue.Count > 2;
        }
    }
}
