using System;

namespace _3dTerrainGeneration.Engine.Util
{
    internal class ArrayCollection<T>
    {
        private T[] array;
        public int PopulatedCount = 0;

        public ArrayCollection(int maxElements)
        {
            array = new T[maxElements];
        }

        public void Insert(T value)
        {
            if (PopulatedCount == array.Length)
            {
                throw new InvalidOperationException("Array is full!");
            }

            array[PopulatedCount] = value;

            PopulatedCount++;
        }

        public void RemoveAt(int index)
        {
            PopulatedCount--;

            if (index == PopulatedCount)
            {
                array[index] = default;

                return;
            }

            array[index] = array[PopulatedCount];
            array[PopulatedCount] = default;
        }

        public T this[int i]
        {
            get
            {
                return array[i];
            }
        }
    }
}
