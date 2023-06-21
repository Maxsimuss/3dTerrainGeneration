using _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader.Interfaces;
using System.Text;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models.VoxReader
{
    public class Palette : IPalette
    {
        public Color[] Colors { get; }

        public Palette(Color[] colors)
        {
            Colors = colors;
        }

        public override string ToString()
        {
            var output = new StringBuilder();

            for (int i = 0; i < Colors.Length - 1; i++)
            {
                output.AppendLine(GetText(i));
            }
            output.Append(GetText(Colors.Length - 1));

            string GetText(int index) => $"{index}: [{Colors[index]}]";

            return output.ToString();
        }
    }
}