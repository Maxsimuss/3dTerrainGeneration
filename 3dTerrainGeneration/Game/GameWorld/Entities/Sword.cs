using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Input;
using _3dTerrainGeneration.Engine.Physics;
using System;
using System.Numerics;
using static OpenTK.Mathematics.MathHelper;

namespace _3dTerrainGeneration.Game.GameWorld.Entities
{
    internal class Sword : LivingEntity<Sword>
    {
        private AxisAlignedBB aabb = new AxisAlignedBB(AABB);

        static Sword()
        {
            MeshedModel data = ModelLoader.Load("sword");
            MeshScale = 1.42f / data.Height;

            AABB = new AxisAlignedBBPrototype(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public Sword(World world, int entityId) : base(world, entityId)
        {
        }

        public override void Tick()
        {
            base.Tick();
            aabb.SetPositionCenteredXZ(Position);
        }
    }
}
