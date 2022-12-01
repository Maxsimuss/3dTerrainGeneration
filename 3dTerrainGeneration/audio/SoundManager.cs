using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.world;
using OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.audio
{
    public enum SoundType
    {
        Forest = 0,
        Walk = 1,
        Fire = 2,
        Explosion = 3,
        ClickHigh = 4,
        ClickLow = 5,
        ClickConfirm = 6,
    }

    public class SoundManager
    {
        private GameSettings gameSettings;
        private Random rnd = new Random();
        private Dictionary<SoundType, List<ALBuffer>> buffers = new Dictionary<SoundType, List<ALBuffer>>();

        private IntPtr device, context;
        private int MaxSources;
        private List<SoundSource> sources = new List<SoundSource>();
        public SoundManager(GameSettings gameSettings)
        {
            this.gameSettings = gameSettings;
            device = ALC10.alcOpenDevice(null);
            context = ALC10.alcCreateContext(device, null);
            ALC10.alcMakeContextCurrent(context);

            AL10.alListenerf(EFX.AL_METERS_PER_UNIT, .3f);
            int[] data = new int[1];
            ALC10.alcGetIntegerv(device, ALC11.ALC_MONO_SOURCES, 1, data);
            MaxSources = data[0];

            AL11.alSpeedOfSound(660);

            foreach (var item in Enum.GetValues<SoundType>())
            {
                buffers.Add(item, new List<ALBuffer>());
            }

            LoadFiles();
        }

        private void LoadFiles()
        {
            Task.Run(() =>
            {
                LoadFiles(SoundType.Forest, "forest.mp3");
                LoadFiles(SoundType.Walk, "step-grass-0.mp3", "step-grass-1.mp3", "step-grass-2.mp3", "step-grass-3.mp3", "step-grass-4.mp3");
                LoadFiles(SoundType.Fire, "fire.mp3");
                LoadFiles(SoundType.Explosion, "explosion.mp3");
                LoadFiles(SoundType.ClickHigh, "click/high-0.mp3", "click/high-1.mp3", "click/high-2.mp3", "click/high-3.mp3");
                LoadFiles(SoundType.ClickLow, "click/low-0.mp3", "click/low-1.mp3", "click/low-2.mp3", "click/low-3.mp3");
                LoadFiles(SoundType.ClickConfirm, "click/confirm-0.mp3", "click/confirm-1.mp3", "click/confirm-2.mp3", "click/confirm-3.mp3");
            });
        }

        private void LoadFiles(SoundType type, params string[] files)
        {
            foreach (var file in files)
            {
                buffers[type].Add(GenBufferFromFile("Resources/sounds/" + file));
            }
        }

        private ALBuffer GetBuffer(SoundType type)
        {
            List<ALBuffer> _buffers = buffers[type];

            if (_buffers.Count < 1) return new ALBuffer();

            return _buffers[rnd.Next(_buffers.Count)];
        }

        private ALBuffer GenBufferFromFile(string file)
        {
            MP3Sharp.MP3Stream stream = new MP3Sharp.MP3Stream(file);

            int samplerate = stream.Frequency;
            List<byte> decodedData = new List<byte>();
            byte[] buffer = new byte[samplerate * 2];
            while (!stream.IsEOF)
            {
                stream.Read(buffer, 0, samplerate * 2);
                decodedData.AddRange(buffer);
            }

            buffer = decodedData.ToArray();

            short[] soundData = new short[buffer.Length / 4];
            for (int i = 0; i < buffer.Length; i += 4)
            {
                short l = BitConverter.ToInt16(buffer, i);
                short r = BitConverter.ToInt16(buffer, i + 2);
                short m = (short)((l + r) / 2);
                soundData[i / 4] = m;
            }

            return SoundSource.GenBuffer(soundData, samplerate);
        }

        public void Update(Camera camera, double fT)
        {
            AL10.alListenerf(AL10.AL_GAIN, gameSettings.Volume);


            float yaw = camera.Yaw + 90;
            float[] directionvect = new float[6];
            directionvect[0] = (float)Math.Sin(Math.PI * yaw / 180.0);
            directionvect[1] = 0;
            directionvect[2] = -(float)Math.Cos(Math.PI * yaw / 180.0);
            directionvect[3] = 0;
            directionvect[4] = 1;
            directionvect[5] = 0;


            AL10.alListenerfv(AL10.AL_ORIENTATION, directionvect);
            AL10.alListener3f(AL10.AL_POSITION, camera.Position.X, camera.Position.Y, camera.Position.Z);
            AL10.alListener3f(AL10.AL_VELOCITY, camera.Velocity.X, camera.Velocity.Y, camera.Velocity.Z);

            Vector3 listenerPosition = camera.Position;

            lock (sourceLock)
                sources.RemoveAll((s) =>
                {
                    if (s == null) return true;
                    if (!s.Loop && s.TTL < 0)
                    {
                        s.Stop();
                        return true;
                    }
                    return false;
                });

            lock (sourceLock)
                sources.RemoveAll((s) =>
                {
                    if (Math.Abs(s.position.X - camera.Position.X) > gameSettings.View_Distance + Chunk.Size * 2 ||
                        Math.Abs(s.position.Y - camera.Position.Y) > gameSettings.View_Distance + Chunk.Size * 2 ||
                        Math.Abs(s.position.Z - camera.Position.Z) > gameSettings.View_Distance + Chunk.Size * 2)
                    {
                        s.Stop();
                        return true;
                    }
                    return false;
                });

            lock (sourceLock)
                sources = sources.OrderBy(o => o.Relative ? 0 : o.DistanceToSq(listenerPosition)).ToList();

            lock (sourceLock)
                for (int i = sources.Count - 1; i >= 0; i--)
                {
                    if (i > MaxSources)
                    {
                        sources[i].Stop();
                    }
                    else
                    {
                        sources[i].Play();
                    }
                }


            lock (sourceLock)
                foreach (var s in sources)
                {
                    if (!s.Loop)
                        s.TTL -= fT;
                }
        }

        object sourceLock = new object();
        public SoundSource PlaySound(SoundType type, Vector3 position, bool loop, float pitch = 1, float volume = 1)
        {
            SoundSource source = new SoundSource(position, GetBuffer(type), loop, pitch, volume);

            lock (sourceLock)
                sources.Add(source);

            return source;
        }

        public SoundSource PlaySound(SoundType type, bool loop, float pitch = 1, float volume = 1)
        {
            SoundSource source = new SoundSource(GetBuffer(type), loop, pitch, volume);

            lock (sourceLock)
                sources.Add(source);

            return source;
        }

        public void Dispose()
        {
            ALC10.alcMakeContextCurrent(IntPtr.Zero);
            ALC10.alcDestroyContext(context);

            if (device != IntPtr.Zero)
            {
                ALC10.alcCloseDevice(device);
            }
            device = IntPtr.Zero;
        }
    }
}
