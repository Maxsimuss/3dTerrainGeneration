namespace _3dTerrainGeneration.Engine.Graphics.Backend.RenderActions
{
    internal class StartProfilerSectionAction : IRenderAction
    {
        private string name;

        public StartProfilerSectionAction(string name)
        {
            this.name = name;
        }

        public void Apply()
        {
            GPUProfilter.Instance.StartSection(name);
        }
    }
}
