using System;
using System.Collections.Generic;
using System.Numerics;
using static OpenTK.Mathematics.MathHelper;

namespace _3dTerrainGeneration.Engine.Physics
{
    public class PhysicsEngine
    {
        private static readonly int MAX_ENTITIES = 4096;
        //private static readonly int MAX_FRAMES = 128;

        private static PhysicsEngine current = null;
        public static PhysicsEngine Current
        {
            get
            {
                if (current == null)
                {
                    current = new PhysicsEngine();
                }

                return current;
            }
        }

        public Dictionary<int, EntityPhysicsData[]> entityData = new Dictionary<int, EntityPhysicsData[]>();
        public Queue<PhysicsInputData>[] inputData = new Queue<PhysicsInputData>[MAX_ENTITIES];

        public int CurrentFrame = 0;
        public int ResimulationFrame = -1;

        public ref EntityPhysicsData GetDataForEntity(int id)
        {
            return ref entityData[CurrentFrame][id];
        }

        public PhysicsEngine()
        {
            for (int i = 0; i < MAX_ENTITIES; i++)
            {
                inputData[i] = new Queue<PhysicsInputData>();
            }
        }

        public void SetEntityInputAtFrame(PhysicsInputData input, int entityId, int frame)
        {
            //CurrentFrame = Math.Max(CurrentFrame, frame);

            int rollbackFrames = CurrentFrame - frame;

            //if (rollbackFrames >= MAX_FRAMES)
            //{
            //    Console.WriteLine("Unable to resimulate!");
            //    return;
            //}

            //if (ResimulationFrame != -1)
            //{
            //    ResimulationFrame = Math.Min(frame, ResimulationFrame);
            //}
            //else
            //{
            //    ResimulationFrame = frame;
            //}

            //for (int i = 0; i <= rollbackFrames + 10; i++)
            //{
            //    if (!inputData.ContainsKey(frame + i))
            //    {
            //        inputData.Add(frame + i, new PhysicsInputData[MAX_ENTITIES]);
            //    }

            //    inputData[frame + i][entityId] = input;
            //}

            inputData[entityId].Enqueue(input);
        }

        public void SimulateNextFrame()
        {
            int simulationCount = 1;

            //if (ResimulationFrame != -1)
            //{
            //    simulationCount = CurrentFrame - ResimulationFrame + 1;
            //    CurrentFrame = ResimulationFrame;
            //    Console.WriteLine(simulationCount);

            //    ResimulationFrame = -1;
            //}

            if (!entityData.ContainsKey(CurrentFrame))
            {
                entityData.Add(CurrentFrame, new EntityPhysicsData[MAX_ENTITIES]);
            }

            for (int simulationIter = 0; simulationIter < simulationCount; simulationIter++)
            {
                EntityPhysicsData[] currentData = entityData[CurrentFrame];
                EntityPhysicsData[] nextState = new EntityPhysicsData[MAX_ENTITIES];

                Array.Copy(currentData, nextState, MAX_ENTITIES);

                for (int i = 0; i < MAX_ENTITIES; i++)
                {
                    ref EntityPhysicsData data = ref nextState[i];
                    PhysicsInputData input = new PhysicsInputData();
                    if (inputData[i].Count == 1)
                    {
                        input = inputData[i].Peek();
                    }
                    else if (inputData[i].Count > 1)
                    {
                        input = inputData[i].Dequeue();
                    }

                    data.LastPosition = data.Position;
                    data.LastYaw = data.Yaw;
                    data.LastPitch = data.Pitch;

                    data.Yaw = input.Yaw;
                    data.Pitch = input.Pitch;

                    Vector3 inputRotated = new(
                        MathF.Cos(DegreesToRadians(data.Yaw)) * input.Movement.Z + MathF.Sin(DegreesToRadians(-data.Yaw)) * input.Movement.X,
                        input.Movement.Y,
                        MathF.Cos(DegreesToRadians(-data.Yaw)) * input.Movement.X + MathF.Sin(DegreesToRadians(data.Yaw)) * input.Movement.Z
                    );

                    data.Velocity += inputRotated;

                    data.Position += data.Velocity;
                    data.Velocity *= .1f;
                }

                CurrentFrame++;

                entityData[CurrentFrame] = nextState;
            }
        }
    }
}
