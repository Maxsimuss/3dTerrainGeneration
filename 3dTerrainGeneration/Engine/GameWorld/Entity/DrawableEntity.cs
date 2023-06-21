﻿using _3dTerrainGeneration.Engine.Audio;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Physics;
using _3dTerrainGeneration.Engine.Graphics._3D;
using System.Numerics;
using TerrainServer.network;
using _3dTerrainGeneration.Engine.Graphics;

namespace _3dTerrainGeneration.Engine.World.Entity
{
    internal abstract class DrawableEntity<T> : EntityBase, IDrawableEntity
    {
        protected static InderectDraw[] InderectDraws;
        protected static VertexData[][] Mesh;
        protected static float MeshScale = 1;

        public bool Visible = true;

        protected int AnimationFrame = 0;
        protected Vector3 InterpolatedPosition => Position * GraphicsEngine.Instance.TickFraction + LastPosition * (1 - GraphicsEngine.Instance.TickFraction);
        protected virtual Matrix4x4 ModelMatrix => Matrix4x4.CreateRotationX((float)OpenTK.Mathematics.MathHelper.DegreesToRadians(-Pitch)) * Matrix4x4.CreateRotationY((float)OpenTK.Mathematics.MathHelper.DegreesToRadians(-Yaw)) * Matrix4x4.CreateTranslation(InterpolatedPosition);


        public DrawableEntity(IWorld world, int id) : base(world, id)
        {
        }

        public virtual void Render()
        {
            if (!Visible) return;

            if (InderectDraws == null)
            {
                InderectDraws = new InderectDraw[Mesh.Length];
                for (int i = 0; i < Mesh.Length; i++)
                {
                    InderectDraws[i] = SceneRenderer.Instance.SubmitMesh(Mesh[i]);
                }
            }

            InderectDraw draw = InderectDraws[AnimationFrame];
            SceneRenderer.Instance.QueueRender(draw, ModelMatrix);
        }
    }
}