using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Game.GameWorld.Workers
{
    internal class ChunkMeshWorker : IWorker<Chunk>
    {
        private ConcurrentQueue<Chunk> queue = new ConcurrentQueue<Chunk>();

        public ChunkMeshWorker()
        {
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
                        chunk.Mesh();
                        chunk.State &= ~ChunkState.AwaitingMeshGeneration;

                        //chunk.Blocks.Compress();
                    }
                }
            }).ContinueWith(state =>
            {
                if(state.Exception != null)
                    throw state.Exception;
            });
        }

        public void SubmitWork(Chunk chunk)
        {
            chunk.State |= ChunkState.AwaitingMeshGeneration;
            queue.Enqueue(chunk);
        }

        public bool IsBusy()
        {
            return queue.Count > 1;
        }
    }
}
