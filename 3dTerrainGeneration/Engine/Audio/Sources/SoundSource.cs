using _3dTerrainGeneration.Engine.Audio.OpenAL;
using System;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Audio.Sources
{
    internal class SoundSource : ISoundSource
    {
        public bool IsPlaying, Loop, Relative = false;
        public double TTL { get; set; }
        AudioBuffer buffer;

        private Vector3 position;
        public Vector3 Position
        {
            get => position; 
            set
            {
                position = value;

                if (IsPlaying)
                {
                    AL10.alSource3f(source, AL10.AL_POSITION, position.X, position.Y, position.Z);
                }
            }
        }

        private Vector3 velocity;
        public Vector3 Velocity
        {
            get => velocity;
            set
            {
                velocity = value;

                if (IsPlaying)
                {
                    AL10.alSource3f(source, AL10.AL_VELOCITY, velocity.X, velocity.Y, velocity.Z);
                }
            }
        }

        private float pitch, volume;
        uint source;

        public SoundSource(AudioBuffer buffer, bool loop, float pitch, float volume)
        {
            Relative = true;
            this.buffer = buffer;
            Loop = loop;
            this.pitch = pitch;
            this.volume = volume;
            TTL = buffer.length;
        }

        public SoundSource(AudioBuffer buffer, Vector3 position, bool loop, float pitch, float volume)
        {
            this.position = position;
            this.buffer = buffer;
            Loop = loop;
            this.pitch = pitch;
            this.volume = volume;
            TTL = buffer.length;
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

        public void Stop()
        {
            if (!IsPlaying) return;

            AL10.alSourceStop(source);
            AL10.alDeleteSources(1, ref source);

            IsPlaying = false;
        }
    }
}
