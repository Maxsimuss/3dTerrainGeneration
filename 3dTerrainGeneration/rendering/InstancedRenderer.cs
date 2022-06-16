using _3dTerrainGeneration.entity;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network;

namespace _3dTerrainGeneration.rendering
{
    public static class EntityExtensions
    {
        public static ushort[][] GetMesh(this EntityType type)
        {
            switch (type)
            {
                case EntityType.BlueSlime:
                    return BlueSlime.mesh;
                case EntityType.Demon:
                    return Demon.mesh;
                case EntityType.FireBall:
                    return FireBall.mesh;
                case EntityType.Frog:
                    return Frog.mesh;
                case EntityType.Player:
                    return Player.mesh;
                case EntityType.Spider:
                    return Spider.mesh;
                default:
                    throw new Exception("Unknown packet received");
            }
        }
    }

    public class InstancedRenderer
    {
        Dictionary<EntityType, ModelInstance[]> draws = new Dictionary<EntityType, ModelInstance[]>();

        public InstancedRenderer()
        {
            foreach (var type in Enum.GetValues<EntityType>())
            {
                ushort[][] mesh = type.GetMesh();

                draws[type] = new ModelInstance[mesh.Length];
                for (int i = 0; i < mesh.Length; i++)
                {
                    draws[type][i] = new ModelInstance(mesh[i]);
                }
            }
        }

        public void Submit(EntityType type, int animationFrame, Matrix4 matrix)
        {
            draws[type][animationFrame].Add(matrix);
        }

        public void Render(Shader shader)
        {
            shader.Use();
            foreach (var type in draws.Keys)
            {
                foreach (var item in draws[type])
                {
                    item.Render();
                }
            }
        }
    }
}
