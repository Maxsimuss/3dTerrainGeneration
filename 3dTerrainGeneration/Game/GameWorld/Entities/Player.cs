using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Input;
using _3dTerrainGeneration.Engine.Physics;
using System;
using System.Numerics;
using static OpenTK.Mathematics.MathHelper;

namespace _3dTerrainGeneration.Game.GameWorld.Entities
{
    internal class Player : LivingEntity<Player>, IEntityInputHandler
    {
        static Player()
        {
            MeshedModel data = ModelLoader.Load("player");
            MeshScale = 3f / data.Height;

            AABB = new AxisAlignedBBPrototype(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public Player(World world, int entityId) : base(world, entityId)
        {
        }

        public void HandleInput(InputState input)
        {
            Yaw += input.Yaw;
            Pitch -= input.Pitch;

            Vector3 inputRotated = new(
                MathF.Sin(DegreesToRadians(-Yaw)) * input.Movement.Z + MathF.Cos(DegreesToRadians(Yaw)) * input.Movement.X,
                input.Movement.Y,
                MathF.Sin(DegreesToRadians(Yaw)) * input.Movement.X + MathF.Cos(DegreesToRadians(-Yaw)) * input.Movement.Z
            );
            Velocity += inputRotated;
        }

        public override void Tick()
        {
            LastPosition = Position;

            Position += Velocity;
            Velocity *= .1f;
        }
    }
}
