namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models
{
    internal struct MeshedModel
    {
        public int Width, Height;
        public VertexData[][] Data;

        public MeshedModel(int width, int height, VertexData[][] data) : this()
        {
            Width = width;
            Height = height;
            Data = data;
        }
    }
}
