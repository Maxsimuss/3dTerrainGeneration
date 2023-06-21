using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader
{
    public class Model : IModel
    {
        public Vector3 Size { get; }
        public Voxel[] Voxels { get; }
        public int Id { get; }
        public bool IsCopy { get; }

        public Model(int id, Vector3 size, Voxel[] voxels, bool isCopy)
        {
            Id = id;
            Size = size;
            Voxels = voxels;
            IsCopy = isCopy;
        }

        internal Model GetCopy()
        {
            return new(Id, Size, Voxels, true);
        }

        public override string ToString()
        {
            return $"Id: {Id}, Size: {Size}, Voxel Count: {Voxels.Length}";
        }
    }
}