using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    internal class NameGenerator
    {
        private static Random rnd = new();
        public static string GetAreaName()
        {
            string name = "";

            switch (rnd.Next(4))
            {
                case 0: name += "Lands "; break;
                case 1: name += "Mountains "; break;
                case 2: name += "Flats "; break;
                case 3: name += "Deadlands "; break;
            }

            name += "of ";

            switch (rnd.Next(11))
            {
                case 0: name += "Fo"; break;
                case 1: name += "Lo"; break;
                case 2: name += "Ro"; break;
                case 3: name += "Ko"; break;
                case 4: name += "No"; break;
                case 5: name += "Ku"; break;
                case 6: name += "Ki"; break;
                case 7: name += "Ra"; break;
                case 8: name += "Ru"; break;
                case 9: name += "Ne"; break;
                case 10: name += "Ri"; break;
            }

            int iter = rnd.Next(2, 5);
            for (int i = 0; i < iter && name.Length <= 18; i++)
            {
                switch (rnd.Next(22))
                {
                    case 0: name += "fo"; break;
                    case 1: name += "lo"; break;
                    case 2: name += "ro"; break;
                    case 3: name += "ko"; break;
                    case 4: name += "no"; break;
                    case 5: name += "ku"; break;
                    case 6: name += "ki"; break;
                    case 7: name += "ra"; break;
                    case 8: name += "ru"; break;
                    case 9: name += "ne"; break;
                    case 10: name += "ri"; break;
                    case 11: name += "fon"; break;
                    case 12: name += "lok"; break;
                    case 13: name += "rom"; break;
                    case 14: name += "kor"; break;
                    case 15: name += "noi"; break;
                    case 16: name += "kui"; break;
                    case 17: name += "kir"; break;
                    case 18: name += "ral"; break;
                    case 19: name += "rua"; break;
                    case 20: name += "ner"; break;
                    case 21: name += "rik"; break;
                }
            }

            return name;
        }
    }
}
