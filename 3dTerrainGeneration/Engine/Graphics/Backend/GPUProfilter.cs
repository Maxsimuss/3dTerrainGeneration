using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _3dTerrainGeneration.Engine.Graphics.Backend
{
    internal class ProfilerFrame
    {
        public List<string> queryNames = new List<string>();
        public List<int> startQueries = new List<int>();
        public List<int> endQueries = new List<int>();
    }

    internal class GPUProfilter
    {
        private static GPUProfilter instance;
        public static GPUProfilter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GPUProfilter();
                }

                return instance;
            }
        }

        private string sectionName = null;

        private int[] temp = new int[2];
        private Queue<int> queryBuffer = new Queue<int>();
        private Queue<ProfilerFrame> frames = new Queue<ProfilerFrame>();
        private Dictionary<string, float> sectionValues = new Dictionary<string, float>();
        private ProfilerFrame frame = null;

        private GPUProfilter()
        {

        }

        public void BeginFrame()
        {
            frame = new ProfilerFrame();
        }

        public void StartSection(string name)
        {
            if (sectionName != null)
            {
                EndSection();
            }

            sectionName = name;
            int query;
            if (queryBuffer.Count > 0)
            {
                query = queryBuffer.Dequeue();
            }
            else
            {
                GL.GenQueries(2, temp);
                query = temp[0];
                queryBuffer.Enqueue(temp[1]);
            }

            GL.QueryCounter(query, QueryCounterTarget.Timestamp);

            frame.startQueries.Add(query);
            frame.queryNames.Add(name);
        }

        public void EndSection()
        {
            if (sectionName == null) throw new Exception("Tried to end a nonexistent GPUProfiler section!");

            int query = queryBuffer.Dequeue();
            GL.QueryCounter(query, QueryCounterTarget.Timestamp);
            frame.endQueries.Add(query);

            sectionName = null;
        }

        public double GetTime(string name)
        {
            if (frame != null)
            {
                frames.Enqueue(frame);
                frame = null;
            }
            ProfilerFrame _frame = frames.Peek();
            for (int i = 0; i < _frame.startQueries.Count; i++)
            {
                int q0 = _frame.endQueries[i];
                int available;
                GL.GetQueryObject(q0, GetQueryObjectParam.QueryResultAvailable, out available);
                if (available == 0)
                {
                    break;
                }

                int q1 = _frame.startQueries[i];

                long start, end;
                GL.GetQueryObject(q0, GetQueryObjectParam.QueryResult, out end);
                GL.GetQueryObject(q1, GetQueryObjectParam.QueryResult, out start);

                double time = (end - start) / 1000000.0;

                if (_frame.queryNames[i] == name)
                {
                    return time;
                }
            }

            return 0;
        }

        public void EndFrame()
        {
            frames.Enqueue(frame);
        }

        public List<string> GetTimes()
        {
            List<string> times = new List<string>();
            if (frames.Count < 1)
            {
                return times;
            }
            ProfilerFrame _frame = frames.Peek();
            double total = 0;
            bool invalid = false;
            for (int i = 0; i < _frame.startQueries.Count; i++)
            {
                int q0 = _frame.endQueries[i];
                int available;
                GL.GetQueryObject(q0, GetQueryObjectParam.QueryResultAvailable, out available);
                if (available == 0)
                {
                    invalid = true;
                    break;
                }

                int q1 = _frame.startQueries[i];

                long start, end;
                GL.GetQueryObject(q0, GetQueryObjectParam.QueryResult, out end);
                GL.GetQueryObject(q1, GetQueryObjectParam.QueryResult, out start);

                double time = (end - start) / 1000000.0;
                if (!sectionValues.ContainsKey(_frame.queryNames[i]))
                    sectionValues.Add(_frame.queryNames[i], (float)time);

                //sectionValues[_frame.queryNames[i]] = (float)time;

                sectionValues[_frame.queryNames[i]] += (float)time * .01f;
                sectionValues[_frame.queryNames[i]] /= 1.01f;
                total += time;
            }

            if (!invalid)
            {
                for (int i = 0; i < _frame.startQueries.Count; i++)
                {
                    queryBuffer.Enqueue(_frame.endQueries[i]);
                    queryBuffer.Enqueue(_frame.startQueries[i]);
                }

                frames.Dequeue();
            }
            if (!sectionValues.ContainsKey("GPU Time"))
                sectionValues.Add("GPU Time", (float)total);
            sectionValues["GPU Time"] = (float)total;
            //sectionValues["GPU Time"] += (float)total * .01f;
            //sectionValues["GPU Time"] /= 1.01f;


            foreach (var item in sectionValues.OrderBy(e => (int)(e.Value * 100) / 100f))
            {
                times.Add(item.Key + ": " + (int)(item.Value * 100) / 100f + "ms");
            }

            return times;
        }
    }
}
