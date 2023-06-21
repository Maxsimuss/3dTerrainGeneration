using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Options
{
    internal class OptionManager
    {
        private static OptionManager instance;
        public static OptionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new OptionManager();
                }


                return instance;
            }
        }

        private Dictionary<string, Dictionary<string, Option>> options = new Dictionary<string, Dictionary<string, Option>>();

        public OptionManager()
        {

        }

        public void RegisterCategory(string category)
        {
            options.Add(category, new Dictionary<string, Option>());
        }

        public void UnregisterCategory(string category)
        {
            options.Remove(category);
        }

        public void RegisterOption(string category, string setting, double min, double max, double value)
        {
            options[category].Add(setting, new Option(min, max, value));
        }

        public void UnregisterOption(string categoty, string setting)
        {
            options[categoty].Remove(setting);
        }

        public double this[string category, string setting]
        {
            get
            {
                return options[category][setting].Value;
            }
            set
            {
                options[category][setting].Value = value;
            }
        }
    }
}
