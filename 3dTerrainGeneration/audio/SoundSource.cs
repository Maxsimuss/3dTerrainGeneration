using _3dTerrainGeneration.world;
using OpenAL;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.audio
{
    public struct ALBuffer
    {
        public ALBuffer(uint buffer, double length, int sampleRate)
        {
            this.buffer = buffer;
            this.length = length;
            this.sampleRate = sampleRate;
        }

        public uint buffer;
        public double length;
        public int sampleRate;
    }

    public class SoundSource
    {
        public bool IsPlaying, Loop, Relative = false;
        public double TTL;
        ALBuffer buffer;

        public Vector3 position;
        public Vector3 velocity;
        private float pitch, volume;
        uint source;

        public SoundSource(ALBuffer buffer, bool loop, float pitch, float volume)
        {
            Relative = true;
            this.buffer = buffer;
            this.Loop = loop;
            this.pitch = pitch;
            this.volume = volume;
            TTL = buffer.length;
        }

        public SoundSource(Vector3 position, ALBuffer buffer, bool loop, float pitch, float volume)
        {
            this.position = position;
            this.buffer = buffer;
            this.Loop = loop;
            this.pitch = pitch;
            this.volume = volume;
            TTL = buffer.length;
        }

        public static ALBuffer GenBuffer(short[] data, int sampleFreq)
        {
            byte[] byteData = new byte[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
            {
                byte[] d = BitConverter.GetBytes(data[i]);

                byteData[i * 2] = d[0];
                byteData[i * 2 + 1] = d[1];
            }

            uint buffer;
            AL10.alGenBuffers(1, out buffer);
            AL10.alBufferData(buffer, AL10.AL_FORMAT_MONO16, byteData, byteData.Length * sizeof(byte), sampleFreq);

            return new ALBuffer(buffer, data.Length / (double)sampleFreq, sampleFreq);
        }

        public void Play()
        {
            if (IsPlaying) return;

            AL10.alGenSources(1, out source);

            AL10.alSourcei(source, AL10.AL_BUFFER, (int)buffer.buffer);
            AL10.alSourcei(source, AL10.AL_LOOPING, Loop ? 1 : 0);
            AL10.alSourcei(source, AL10.AL_SOURCE_RELATIVE, Relative ? 1 : 0);
            AL10.alSource3f(source, AL10.AL_POSITION, position.X, position.Y, position.Z);
            AL10.alSource3f(source, AL10.AL_VELOCITY, velocity.X, velocity.Y, velocity.Z);
            AL10.alSourcef(source, AL10.AL_GAIN, volume);
            AL10.alSourcef(source, AL10.AL_PITCH, pitch);

            AL10.alSourcef(source, EFX.AL_AIR_ABSORPTION_FACTOR, 1);
            AL10.alSourcef(source, AL10.AL_DOPPLER_FACTOR, 1);

            if (Loop)
                AL10.alSourcef(source, AL11.AL_SEC_OFFSET, (float)(new Random().NextDouble() * buffer.length));
            else
                AL10.alSourcei(source, AL11.AL_SAMPLE_OFFSET, (int)(Math.Min(buffer.length - 1d / buffer.sampleRate, buffer.length - Math.Max(0.0001, TTL)) * buffer.sampleRate));

            AL10.alSourcePlay(source);

            IsPlaying = true;
        }

        public void SetVelocity(Vector3 v)
        {
            velocity = v;
            if (IsPlaying)
            {
                AL10.alSource3f(source, AL10.AL_VELOCITY, velocity.X, velocity.Y, velocity.Z);
            }
        }

        public void SetPosition(Vector3 pos)
        {
            position = pos;
            if(IsPlaying)
            {
                AL10.alSource3f(source, AL10.AL_POSITION, position.X, position.Y, position.Z);
            }
        }

        public void Stop()
        {
            if (!IsPlaying) return;
            
            AL10.alSourceStop(source);
            AL10.alDeleteSources(1, ref source);
            
            IsPlaying = false;
        }

        public float DistanceToSq(Vector3 pos)
        {
            return (position - pos).LengthSquared();
        }
    }
}
