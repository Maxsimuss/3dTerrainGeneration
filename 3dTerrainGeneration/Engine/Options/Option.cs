using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Options
{
    internal class Option
    {
        private double min;
        private double max;

        private double value;

        public Option(double min, double max, double value)
        {
            this.min = min;
            this.max = max;
            Value = value;
        }

        public double Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = Math.Clamp(value, min, max);
            }
        }
    }
}
