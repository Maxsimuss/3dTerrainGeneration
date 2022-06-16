using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.audio
{
    class SoundManager
    {
        IntPtr device;
        ContextHandle context;

        public List<SoundSource> sources = new List<SoundSource>();
        public unsafe SoundManager()
        {
            device = Alc.OpenDevice(null);
			context = Alc.CreateContext(device, (int*)null);
			Alc.MakeContextCurrent(context);

            AL.DistanceModel(ALDistanceModel.InverseDistance);
            AL.Listener(ALListenerf.Gain, 1f);
        }

        Vector3 listenerPosition = new Vector3(0);

        public void Update(Camera camera, double fT)
        {
            float yaw = camera.Yaw + 90;
            float[] directionvect = new float[6];
            directionvect[0] = (float)Math.Sin(Math.PI * yaw / 180.0);
            directionvect[1] = 0;
            directionvect[2] = -(float)Math.Cos(Math.PI * yaw / 180.0);
            directionvect[3] = 0;
            directionvect[4] = 1;
            directionvect[5] = 0;

            AL.Listener(ALListenerfv.Orientation, ref directionvect);
            AL.Listener(ALListener3f.Position, ref camera.Position);

            listenerPosition = camera.Position;

            sources.RemoveAll((s) =>
            {
                if(!s.Loop && s.TTL < 0)
                {
                    s.Stop();
                    return true;
                }
                return false;
            });

            sources = sources.OrderBy(o => o.DistanceTo(listenerPosition)).ToList();
            for (int i = sources.Count - 1; i >= 0; i--)
            {
                if (i > 250)
                {
                    sources[i].Stop();
                }
                else
                {
                    sources[i].Play();
                }
            }


            foreach (var s in sources)
            {
                if (!s.Loop)
                    s.TTL -= fT;
            }
        }

        public void PlaySound(Buffer sound, Vector3 position, bool loop, float pitch = 1, bool relative = false)
        {
            SoundSource source = new SoundSource(position, sound, loop, pitch, relative);
            sources.Add(source);
        }

        public void Dispose()
        {
            if (context != ContextHandle.Zero)
            {
                Alc.MakeContextCurrent(ContextHandle.Zero);
                Alc.DestroyContext(context);
            }
            context = ContextHandle.Zero;

            if (device != IntPtr.Zero)
            {
                Alc.CloseDevice(device);
            }
            device = IntPtr.Zero;
        }
    }
}
