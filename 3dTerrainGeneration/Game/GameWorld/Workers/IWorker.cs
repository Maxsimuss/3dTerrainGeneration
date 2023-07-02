namespace _3dTerrainGeneration.Game.GameWorld.Workers
{
    internal interface IWorker<T>
    {
        public void SubmitWork(T work);

        public bool IsBusy();
    }
}
