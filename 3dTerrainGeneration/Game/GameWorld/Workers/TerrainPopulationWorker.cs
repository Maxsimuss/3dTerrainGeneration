using _3dTerrainGeneration.Game.GameWorld.Generators;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Game.GameWorld.Workers
{
    internal class TerrainPopulationWorker : IWorker<Chunk>
    {
        private ConcurrentQueue<Chunk> queue = new ConcurrentQueue<Chunk>();
        private TerrainGenerator generator;
        private ChunkManager chunkManager;
        private Action<Chunk> onFailed;

        public TerrainPopulationWorker(ChunkManager chunkManager, TerrainGenerator generator, Action<Chunk> onFailed)
        {
            this.chunkManager = chunkManager;
            this.generator = generator;
            this.onFailed = onFailed;

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
                        try
                        {
                            generator.Populate(chunk, chunkManager);
                        }
                        catch (InvalidOperationException ex)
                        {
                            onFailed(chunk);
                        }

                        chunk.State &= ~ChunkState.AwaitingTerrainPopulation;
                    }
                }
            });
        }

        public void SubmitWork(Chunk chunk)
        {
            chunk.State |= ChunkState.AwaitingTerrainPopulation;
            queue.Enqueue(chunk);
        }

        public bool IsBusy()
        {
            return queue.Count > 16;
        }
    }
}
