using System;
using System.Collections.Generic;

namespace _3dTerrainGeneration.Engine.Util
{
    internal class PackedArray<T>
    {
        private T[] array;
        public int Count = 0;

        public PackedArray(int maxElements)
        {
            array = new T[maxElements];
        }

        public void Insert(T value)
        {
            if (Count == array.Length)
            {
                throw new InvalidOperationException("Array is full!");
            }

            array[Count] = value;

            Count++;
        }

        public void Remove(T value)
        {
            for (int i = 0; i < Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(array[i], value))
                {
                    RemoveAt(i);
                    break;
                }
            }
        }

        public void RemoveAt(int index)
        {
            Count--;

            if (index == Count)
            {
                array[index] = default;

                return;
            }

            array[index] = array[Count];
            array[Count] = default;
        }

        public void SortOneShot(Comparison<T> comparer)
        {
            T temp = default;
            for (int i = 0; i < Count - 1; i++)
            {
                if (comparer.Invoke(array[i], array[i + 1]) > 0)
                {
                    temp = array[i];
                    array[i] = array[i + 1];
                    array[i + 1] = temp;
                }
            }
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
