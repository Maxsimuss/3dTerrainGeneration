using System.Collections.Generic;
using System.Linq;

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

        public event OptionsChangedEvent OnOptionsChanged;
        public delegate void OptionsChangedEvent(string category, string name);


        private Dictionary<string, Dictionary<string, Option>> options = new Dictionary<string, Dictionary<string, Option>>();

        public OptionManager()
        {

        }

        public void RegisterOption(string category, string name, Option option)
        {
            if (!options.ContainsKey(category))
            {
                options.Add(category, new Dictionary<string, Option>());
            }

            if (!options[category].ContainsKey(name))
            {
                options[category].Add(name, option);
            }
        }

        public void UnregisterOption(string category, string name)
        {
            options[category].Remove(name);
        }

        public void UnregisterCategory(string category)
        {
            options.Remove(category);
        }

        public Option this[string category, string name]
        {
            get
            {
                return options[category][name];
            }
        }

        public List<string> ListCategories()
        {
            return options.Keys.ToList();
        }

        public Dictionary<string, Option> ListOptionsForCategoty(string category)
        {
            return options[category].ToDictionary(e => e.Key, e => e.Value);
        }
    }
}
