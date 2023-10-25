namespace _3dTerrainGeneration.Engine.Options
{
    internal class BoolOption : Option
    {
        private bool value;

        public BoolOption(bool defaultValue)
        {
            value = defaultValue;
        }

        public override object Value
        {
            get => value;
            set
            {
                this.value = (bool)value;
                OnChanged();
            }
        }
    }

    internal abstract partial class Option
    {
        public static implicit operator bool(Option option) => (bool)option.Value;
    }
}
