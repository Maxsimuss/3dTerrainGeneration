using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.audio
{
    class SoundType
    {
        public static Buffer FOREST = GenBufferFromFile(@"C:\Users\Maxsimus\Downloads\forest.mp3");
        public static Buffer WALK = GenBufferFromFile(@"C:\Users\Maxsimus\Downloads\walk.mp3");

        private static Buffer GenBufferFromFile(string file)
        {
            MP3Sharp.MP3Stream stream = new MP3Sharp.MP3Stream(file);


            int samplerate = stream.Frequency;
            List<byte> decodedData = new List<byte>();
            byte[] buffer = new byte[samplerate];
            while (!stream.IsEOF)
            {
                stream.Read(buffer, 0, samplerate);
                decodedData.AddRange(buffer);
            }

            buffer = decodedData.ToArray();

            byte[] soundData = new byte[buffer.Length / 2];
            for (int i = 0; i < buffer.Length; i += 4)
            {
                short l = BitConverter.ToInt16(buffer, i);
                short r = BitConverter.ToInt16(buffer, i + 2);
                short m = (short)((l + r) / 2);
                byte[] bytes = BitConverter.GetBytes(m);
                soundData[i / 2] = bytes[0];
                soundData[i / 2 + 1] = bytes[1];
            }

            return SoundSource.GenBuffer(soundData, samplerate);
        }

    }
}
