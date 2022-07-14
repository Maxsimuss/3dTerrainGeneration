﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.util
{
    class Octree
    {
        private byte size, value = 0;
        private Octree[,,] nodes = null;

        public Octree(byte size, byte value)
        {
            this.size = size;
            this.value = value;
        }

        public bool CanMerge(byte value)
        {
            if(nodes == null)
            {
                return value == this.value;
            }

            for (int X = 0; X < 2; X++)
            {
                for (int Y = 0; Y < 2; Y++)
                {
                    for (int Z = 0; Z < 2; Z++)
                    {
                        if(!nodes[X, Y, Z].CanMerge(value))
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

        public void SetValue(byte x, byte y, byte z, byte value)
        {
            if (this.value == value) return;

            if(size > 1)
            {
                int _x = x * 2 / size;
                int _y = y * 2 / size;
                int _z = z * 2 / size;

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

                    nodes[_x, _y, _z].SetValue(x, y, z, value);
                }
                else
                {
                    nodes[_x, _y, _z].SetValue(x, y, z, value);
                    if(CanMerge(value))
                    {
                        this.value = value;
                        nodes = null;
                    }
                }
            }
            else
            {
                this.value = value;
            }
        }

        public byte GetValue(byte x, byte y, byte z)
        {
            if(nodes == null)
            {
                return value;
            }

            int _x = x * 2 / size;
            int _y = y * 2 / size;
            int _z = z * 2 / size;
            
            return nodes[_x, _y, _z].GetValue(x, y, z);
        }
    }
}
