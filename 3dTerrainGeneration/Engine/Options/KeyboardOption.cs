using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace _3dTerrainGeneration.Engine.Options
{
    internal class KeyboardOption : Option
    {
        private Keys value;

        public KeyboardOption(Keys key)
        {
            value = key;
        }

        public override object Value
        {
            get => value;
            set => this.value = (Keys)value;
        }
    }

    internal abstract partial class Option
    {
        public static implicit operator Keys(Option option) => (Keys)option.Value;
    }
}
