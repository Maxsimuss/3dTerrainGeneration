namespace _3dTerrainGeneration.Engine.Graphics.Backend.RenderActions
{
    internal class EndProfilerSectionAction : IRenderAction
    {
        public void Apply()
        {
            GPUProfilter.Instance.EndSection();
        }
    }
}
