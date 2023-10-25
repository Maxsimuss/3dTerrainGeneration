namespace _3dTerrainGeneration.Engine.Options
{
    internal abstract partial class Option
    {
        public event OptionChangeEvent Changed;
        public delegate void OptionChangeEvent();

        public abstract object Value { get; set; }

        protected void OnChanged()
        {
            Changed?.Invoke();
        }
    }
}
