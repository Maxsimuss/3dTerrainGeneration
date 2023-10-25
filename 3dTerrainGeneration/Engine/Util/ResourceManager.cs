using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Util
{
    internal class ResourceManager
    {
        public static string GetResourcePath(string path)
        {
            return Directory.GetCurrentDirectory() + "/Resources/" + path;
        }

        public static byte[] GetResource(string path)
        {
            return File.ReadAllBytes(GetResourcePath(path));
        }

        public static byte[] GetModel(string name)
        {
            return GetResource("Models/" + name);
        }

        public static byte[] GetStructure(string name)
        {
            return GetResource("Models/Structures/" + name);
        }

        public static string GetEntityPath(string name)
        {
            return GetResourcePath("Models/Entities/" + name);
        }

        public static string GetFontPath(string name)
        {
            return GetResourcePath("Fonts/" + name);
        }

        public static string GetShaderSource(string path)
        {
            string source = Encoding.UTF8.GetString(GetResource("Shaders/" + path));

            return source + "\r\n\0";
        }

        public static string GetSoundPath(string name)
        {
            return GetResourcePath("Sounds/" + name);
        }

        public static string GetUserDataPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/VoxelEngine/";
        }
    }
}
