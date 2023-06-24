using _3dTerrainGeneration.Engine.Audio.OpenAL;
using _3dTerrainGeneration.Engine.Audio.Sources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Audio
{
    internal class AudioEngine
    {
        private static AudioEngine instance = null;
        public static AudioEngine Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AudioEngine();
                }

                return instance;
            }
        }

        private Random random = new Random();
        private Dictionary<string, List<AudioBuffer>> buffers = new Dictionary<string, List<AudioBuffer>>();
        private List<ISoundSource> soundSources = new List<ISoundSource>();

        private IntPtr device, context;
        private readonly int MAX_SOURCES;

        private AudioEngine()
        {
            device = ALC10.alcOpenDevice(null);
            context = ALC10.alcCreateContext(device, null);
            ALC10.alcMakeContextCurrent(context);

            int[] data = new int[1];
            ALC10.alcGetIntegerv(device, ALC11.ALC_MONO_SOURCES, 1, data);
            MAX_SOURCES = data[0];

            AL10.alListenerf(EFX.AL_METERS_PER_UNIT, .3f);
            AL11.alSpeedOfSound(343);
        }

        public void Tick(Vector3 position, Vector3 velocity, Vector3 direction, double deltaTime)
        {
            float[] directionvect = new float[6];
            directionvect[0] = direction.X;
            directionvect[1] = direction.Y;
            directionvect[2] = direction.Z;
            directionvect[3] = 0;
            directionvect[4] = 1;
            directionvect[5] = 0;

            AL10.alListenerfv(AL10.AL_ORIENTATION, directionvect);
            AL10.alListener3f(AL10.AL_POSITION, position.X, position.Y, position.Z);
            AL10.alListener3f(AL10.AL_VELOCITY, velocity.X, velocity.Y, velocity.Z);

            soundSources.RemoveAll((source) =>
            {
                if ((source.TTL -= deltaTime / 1000) < 0)
                {
                    source.Stop();
                    return true;
                }
                return false;
            });
        }

        public void RegisterSound(string name, params string[] paths)
        {
            RegisterSound(name, default, paths);
        }

        public void RegisterSound(string name, AudioSourceConfig sourceConfig, params string[] paths)
        {
            foreach (var path in paths)
            {
                RegisterSound(name, sourceConfig, path);
            }
        }

        private void RegisterSound(string name, AudioSourceConfig sourceConfig, string path)
        {
            MP3Sharp.MP3Stream stream = new MP3Sharp.MP3Stream("Resources/sounds/" + path);

            int sampleRate = stream.Frequency;

            List<byte> decodedData = new List<byte>();

            byte[] byteBuffer = new byte[sampleRate * 2];
            while (!stream.IsEOF)
            {
                stream.Read(byteBuffer, 0, sampleRate * 2);
                decodedData.AddRange(byteBuffer);
            }

            byteBuffer = decodedData.ToArray();

            short[] soundData = new short[byteBuffer.Length / 4];
            for (int i = 0; i < byteBuffer.Length; i += 4)
            {
                short l = BitConverter.ToInt16(byteBuffer, i);
                short r = BitConverter.ToInt16(byteBuffer, i + 2);
                short m = (short)((l + r) / 2);
                soundData[i / 4] = m;
            }

            AudioBuffer audioBuffer = new AudioBuffer(soundData, sampleRate, sourceConfig);

            if (!buffers.ContainsKey(name))
            {
                buffers.Add(name, new List<AudioBuffer>());
            }

            buffers[name].Add(audioBuffer);
        }

        public void UnregisterSound(string name)
        {
            buffers.Remove(name);
        }

        public void PlaySound(string name)
        {
            AudioBuffer buffer = buffers[name][random.Next(buffers[name].Count)];

            SoundSource soundSource = new SoundSource(buffer, false, 1.0f, 1.0f);
            soundSource.Play();

            soundSources.Add(soundSource);
        }
    }
}
