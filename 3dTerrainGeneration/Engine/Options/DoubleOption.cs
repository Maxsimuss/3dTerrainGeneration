using LibNoise.Combiner;
using System;
using System.ComponentModel.DataAnnotations;

namespace _3dTerrainGeneration.Engine.Options
{
    internal class DoubleOption : Option
    {
        private double min;
        private double max;
        private double precision;

        private double value;

        public DoubleOption(double min, double max, double value, double precision = 1)
        {
            this.min = min;
            this.max = max;
            if (precision <= 0)
            {
                throw new ArgumentException("Precision must be larger than 0!");
            }

            this.precision = precision;
            Value = value;
        }

        public double ValuePercentage
        {
            get => ((double)Value - min) / (max - min);
            set => Value = (value * (max - min) + min);
        }


        public override object Value
        {
            get
            {
                return value;
            }
            set
            {
                double newValue = Math.Clamp(((int)((double)value / precision)) * precision, min, max);
                if (newValue == this.value)
                {
                    return;
                }

                this.value = newValue;

                OnChanged();
            }
        }
    }

    internal abstract partial class Option
    {
        public static implicit operator double(Option option) => (double)option.Value;
    }
}
