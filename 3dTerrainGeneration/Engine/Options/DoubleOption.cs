using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Options
{
    internal class DoubleOption : Option
    {
        private double min;
        private double max;

        private double value;

        public DoubleOption(double min, double max, double value)
        {
            this.min = min;
            this.max = max;
            Value = value;
        }

        public override object Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = Math.Clamp((double)value, min, max);
            }
        }
    }

    internal abstract partial class Option
    {
        public static implicit operator double(Option option) => (double)option.Value;
    }
}
