using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Input;
using _3dTerrainGeneration.Engine.Physics;
using System;
using System.Numerics;
using static OpenTK.Mathematics.MathHelper;

namespace _3dTerrainGeneration.Game.GameWorld.Entities
{
    internal class Arm : LivingEntity<Arm>
    {
        private AxisAlignedBB aabb = new AxisAlignedBB(AABB);

        protected override Matrix4x4 ModelMatrix => Matrix4x4.CreateScale(MeshScale) * Matrix4x4.CreateTranslation(Offset) * Matrix4x4.CreateTranslation(-AABB.width, 0, -AABB.width) * Matrix4x4.CreateRotationZ((float)OpenTK.Mathematics.MathHelper.DegreesToRadians(-GraphicsEngine.Instance.Lerp(LastPitch, Pitch))) * Matrix4x4.CreateRotationY((float)OpenTK.Mathematics.MathHelper.DegreesToRadians(-GraphicsEngine.Instance.Lerp(LastYaw, Yaw))) * Matrix4x4.CreateTranslation(InterpolatedPosition);

        static Arm()
        {
            MeshedModel data = ModelLoader.Load("arm");
            MeshScale = Player.MeshScale;

            AABB = new AxisAlignedBBPrototype(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public Arm(World world, int entityId) : base(world, entityId)
        {
        }

        public override void Tick()
        {
            base.Tick();
            aabb.SetPositionCenteredXZ(Position);
        }
    }
}
