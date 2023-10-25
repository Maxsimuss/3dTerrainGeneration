using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Input;
using _3dTerrainGeneration.Engine.Physics;
using _3dTerrainGeneration.Engine.Util;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using static OpenTK.Mathematics.MathHelper;

namespace _3dTerrainGeneration.Game.GameWorld.Entities
{
    internal class Player : LivingEntity<Player>, IEntityInputHandler
    {
        private AxisAlignedBB aabb = new AxisAlignedBB(AABB);
        private Sword sword;
        private Arm arm;
        private Arm2 arm2;
        private float swordInputX, swordInputY;

        protected override Matrix4x4 ModelMatrix => Matrix4x4.CreateScale(MeshScale) * Matrix4x4.CreateTranslation(-AABB.width, 0, -AABB.width) * Matrix4x4.CreateRotationY((float)DegreesToRadians(-GraphicsEngine.Instance.Lerp(LastYaw, Yaw))) * Matrix4x4.CreateTranslation(InterpolatedPosition);

        static Player()
        {
            MeshedModel data = ModelLoader.Load("player");
            MeshScale = 3f / data.Height;

            AABB = new AxisAlignedBBPrototype(data.Width * MeshScale / 2f, data.Height * MeshScale);
            Mesh = data.Data;
        }

        public Player(World world, int entityId) : base(world, entityId)
        {
            sword = new Sword(world, entityId + 1);
            arm = new Arm(world, entityId + 2);
            arm2 = new Arm2(world, entityId + 3);
        }

        public void HandleInput(InputState input)
        {
            if (!input.Left)
            {
                Yaw += input.Yaw;
                Pitch -= input.Pitch;

                Pitch = Math.Clamp(Pitch, -90, 90);
            }
            else
            {
                swordInputX += input.Yaw;
                swordInputY -= input.Pitch;
            }

            Vector3 inputRotated = new(
                MathF.Sin(DegreesToRadians(-Yaw)) * input.Movement.Z + MathF.Cos(DegreesToRadians(Yaw)) * input.Movement.X,
                input.Movement.Y,
                MathF.Sin(DegreesToRadians(Yaw)) * input.Movement.X + MathF.Cos(DegreesToRadians(-Yaw)) * input.Movement.Z
            );
            Velocity += inputRotated * .5f;
        }

        public override void Tick()
        {
            Visible = true;
            aabb.SetPositionCenteredXZ(Position);
            base.Tick();

            AxisAlignedBB box = new AxisAlignedBB(new Vector3(40, 40, 40), new Vector3(20, 20, 20));
            if (AxisAlignedBB.Check(aabb, box, Velocity, out float collisionTime, out Vector3 collisionNormal))
            {
                //Velocity *= collisionNormal;
                //Position += Velocity;
                //Position += Velocity * collisionTime * .95f;
                //Position += Velocity * (1 - collisionTime * .95f);
            }
            else
            {
                Position += Velocity;
            }

            sword.Tick();
            arm.Tick();
            arm2.Tick();

            float test = MathF.Sin((float)(TimeUtil.Unix() % 2000) / 1000 * float.Pi) * 45;

            //swordInputY = 0;
            swordInputX = 0;


            float shoulderPitch = Math.Clamp(swordInputY - 90, -90, 75);
            float shoulderYaw = Math.Clamp(swordInputX, -25, 100);
            float forearmPitch = Math.Clamp(swordInputY, 0, 120);
            float forearmYaw = Math.Clamp(swordInputX - shoulderYaw, -90, 0);
            float wristPitch = Math.Clamp(0, -25, 10);
            Console.WriteLine("sh: {0}\nfr: {1}", shoulderPitch, forearmPitch);

            arm.Position = Vector3.Transform(default,
                Matrix4x4.CreateTranslation(-.07f, HitBox.height * .8f, 0.5f) *
                Matrix4x4.CreateRotationY(DegreesToRadians(-Yaw)) *
                Matrix4x4.CreateTranslation(Position)
            );
            arm2.Position = Vector3.Transform(default,
                Matrix4x4.CreateTranslation(.5f, 0, 0) *
                Matrix4x4.CreateRotationY(DegreesToRadians(-shoulderYaw)) *
                Matrix4x4.CreateRotationZ(DegreesToRadians(shoulderPitch)) *
                Matrix4x4.CreateTranslation(-.07f, HitBox.height * .8f, 0.5f) *
                Matrix4x4.CreateRotationY(DegreesToRadians(-Yaw)) *
                Matrix4x4.CreateTranslation(Position)
            );
            sword.Position = Vector3.Transform(default,
                Matrix4x4.CreateTranslation(.5f, 0, 0) *
                Matrix4x4.CreateRotationY(DegreesToRadians(-forearmYaw)) *
                Matrix4x4.CreateRotationZ(DegreesToRadians(forearmPitch)) *
                Matrix4x4.CreateTranslation(.5f, 0, 0) *
                Matrix4x4.CreateRotationY(DegreesToRadians(-shoulderYaw)) *
                Matrix4x4.CreateRotationZ(DegreesToRadians(shoulderPitch)) *
                Matrix4x4.CreateTranslation(-.07f, HitBox.height * .8f, 0.34f) *
                Matrix4x4.CreateRotationY(DegreesToRadians(-Yaw)) *
                Matrix4x4.CreateTranslation(Position)
            );
            sword.Yaw = Yaw - 90;
            arm2.Yaw = Yaw + forearmYaw + shoulderYaw;
            arm2.Pitch = -shoulderPitch - forearmPitch - 90;
            arm2.Offset = new(0, -arm2.HitBox.height, 0);
            arm.Yaw = Yaw + shoulderYaw;
            arm.Offset = new(0, -arm.HitBox.height, 0);
            arm.Pitch = -shoulderPitch - 90;
            sword.Offset = new(0, -.2f, 0.1f);
            sword.Pitch = forearmPitch + shoulderPitch + wristPitch;

            //Console.WriteLine(Position);

            Velocity *= .1f;
        }

        public override void Render()
        {
            base.Render();
            sword.Render();
            arm2.Render();
            arm.Render();
        }
    }
}
