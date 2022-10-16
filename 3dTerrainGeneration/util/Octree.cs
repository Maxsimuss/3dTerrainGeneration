namespace _3dTerrainGeneration.util
{
    public class Octree
    {
        private byte size, value = 0;
        private Octree[,,] nodes = null;

        public Octree(byte size, byte value)
        {
            this.size = size;
            this.value = value;
        }

        public bool Merge(byte value)
        {
            if (nodes == null)
            {
                return value == this.value;
            }

            for (int X = 0; X < 2; X++)
            {
                for (int Y = 0; Y < 2; Y++)
                {
                    for (int Z = 0; Z < 2; Z++)
                    {
                        if (!nodes[X, Y, Z].Merge(value))
                        {
                            return false;
                        }
                    }
                }
            }

            this.value = value;
            nodes = null;

            return true;
        }

        public void SetValue(int x, int y, int z, byte value)
        {
            if (this.value == value) return;

            if (size > 1)
            {
                int _x = x * 2 >= size ? 1 : 0;
                int _y = y * 2 >= size ? 1 : 0;
                int _z = z * 2 >= size ? 1 : 0;

                if (nodes == null)
                {
                    nodes = new Octree[2, 2, 2];

                    for (int X = 0; X < 2; X++)
                    {
                        for (int Y = 0; Y < 2; Y++)
                        {
                            for (int Z = 0; Z < 2; Z++)
                            {
                                nodes[X, Y, Z] = new Octree((byte)(size / 2), this.value);
                            }
                        }
                    }

                }
                nodes[_x, _y, _z].SetValue(x - _x * size / 2, y - _y * size / 2, z - _z * size / 2, value);
                Merge(value);
            }
            else
            {
                this.value = value;
            }
        }

        public byte GetValue(int x, int y, int z)
        {
            if (nodes == null)
            {
                return value;
            }

            int _x = x * 2 >= size ? 1 : 0;
            int _y = y * 2 >= size ? 1 : 0;
            int _z = z * 2 >= size ? 1 : 0;

            return nodes[_x, _y, _z].GetValue(x - _x * size / 2, y - _y * size / 2, z - _z * size / 2);
        }
    }
}
