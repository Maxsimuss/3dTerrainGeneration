using OpenTK;
using OpenAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.audio
{
    struct Buffer
    {
        public Buffer(uint buffer, double length)
        {
            this.buffer = buffer;
            this.length = length;
        }

        public uint buffer;
        public double length;
    }

    class SoundSource
    {
        public bool IsPlaying, Loop;
        public double TTL;
        Buffer buffer;
        uint source;
        public Vector3 position = new Vector3(10, 0, 0);
        private float pitch;
        private bool relative;

        public SoundSource(Vector3 position, Buffer buffer, bool loop, float pitch, bool relative)
        {
            this.position = position;
            this.buffer = buffer;
            this.Loop = loop;
            this.pitch = pitch;
            this.relative = relative;
            TTL = buffer.length;
        }

        public SoundSource(Vector3 position, int sampleFreq, byte[] data)
        {
            this.position = position;
            buffer = GenBuffer(data, sampleFreq);
        }

        public static Buffer GenBuffer(byte[] data, int sampleFreq)
        {
            uint buffer;
            AL10.alGenBuffers(1, out buffer);
            AL10.alBufferData(buffer, AL10.AL_FORMAT_MONO16, data, data.Length * sizeof(byte), sampleFreq);

            Console.WriteLine("Buffer len: {0}, supposed: {1}, splame freq: {2}", data.Length / (double)sampleFreq / 2, 5 * 60, sampleFreq);


            return new Buffer(buffer, data.Length / 2D / sampleFreq);
        }

        public void Play()
        {
            if (IsPlaying) return;

            AL10.alGenSources(1, out source);
            AL10.alSourcei(source, AL10.AL_BUFFER, (int)buffer.buffer);
            AL10.alSourcei(source, AL10.AL_LOOPING, Loop ? 1 : 0);
            AL10.alSource3f(source, AL10.AL_POSITION, position.X, position.Y, position.Z);
            AL10.alSourcef(source, AL10.AL_GAIN, 2f);
            AL10.alSourcei(source, AL10.AL_SOURCE_RELATIVE, relative ? 1 : 0);
            AL10.alSourcef(source, AL10.AL_PITCH, pitch);

            if (Loop)
                AL10.alSourcef(source, AL11.AL_SEC_OFFSET, (float)(new Random().NextDouble() * buffer.length));
            else
            {
                Console.WriteLine("Playign from " + (float)(buffer.length - Math.Max(0.0001, TTL)) + " / " + buffer.length);
                AL10.alSourcef(source, AL11.AL_SEC_OFFSET, (float)(buffer.length - Math.Max(0.0001, TTL)));
            }

            AL10.alSourcePlay(source);
            IsPlaying = true;
        }

        public void Stop()
        {
            if (!IsPlaying) return;

            AL10.alSourceStop(source);
            AL10.alDeleteSources(1, ref source);
            IsPlaying = false;
        }

        public float DistanceTo(Vector3 pos)
        {
            return (position - pos).LengthSquared;
        }
    }
}
