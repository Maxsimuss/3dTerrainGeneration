using _3dTerrainGeneration.Engine.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Audio
{
    internal class AudioBuffer
    {
        public AudioSourceConfig audioSourceConfig;

        public uint buffer;
        public int sampleRate;
        public double length;

        public AudioBuffer(short[] data, int sampleRate, AudioSourceConfig audioSourceConfig)
        {
            this.sampleRate = sampleRate;
            this.length = data.Length / sampleRate;
            this.audioSourceConfig = audioSourceConfig;

            AL10.alGenBuffers(1, out buffer);
            AL10.alBufferData(buffer, AL10.AL_FORMAT_MONO16, data, data.Length * sizeof(short), sampleRate);
        }
    }
}
