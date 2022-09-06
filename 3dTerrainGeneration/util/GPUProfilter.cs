using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.util
{
    internal class Frame
    {
        public List<string> queryNames = new List<string>();
        public List<int> startQueries = new List<int>();
        public List<int> endQueries = new List<int>();
    }

    internal class GPUProfilter
    {
        private string sectionName = null;

        private int[] temp = new int[2];
        private Queue<int> queryBuffer = new Queue<int>();
        private Queue<Frame> frames = new Queue<Frame>();
        Frame frame = null;

        public void BeginFrame()
        {
            frame = new Frame();
        }

        public void Start(string name)
        {
            if (sectionName != null) throw new Exception("Tried to start a GPUProfiler section before ending the previous one!");
            sectionName = name;
            int query;
            if(queryBuffer.Count > 0)
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

        public void End()
        {
            if (sectionName == null) throw new Exception("Tried to end a nonexistent GPUProfiler section!");

            int query = queryBuffer.Dequeue();
            GL.QueryCounter(query, QueryCounterTarget.Timestamp);
            frame.endQueries.Add(query);

            sectionName = null;
        }

        public double GetTime(string name)
        {
            if(frame != null)
            {
                frames.Enqueue(frame);
                frame = null;
            }
            Frame _frame = frames.Peek();
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

                if(_frame.queryNames[i] == name)
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
            if(frames.Count < 1)
            {
                return times;
            }
            Frame _frame = frames.Peek();
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

                times.Add(_frame.queryNames[i] + ": " + time + "ms");
                total += time;
            }

            if(!invalid)
            {
                for (int i = 0; i < _frame.startQueries.Count; i++)
                {
                    queryBuffer.Enqueue(_frame.endQueries[i]);
                    queryBuffer.Enqueue(_frame.startQueries[i]);
                }

                frames.Dequeue();
            }

            times.Add("total: " + total + "ms");

            return times;
        }
    }
}
