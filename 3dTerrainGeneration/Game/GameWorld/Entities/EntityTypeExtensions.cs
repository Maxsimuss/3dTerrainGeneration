using _3dTerrainGeneration.Engine.World.Entity;
using _3dTerrainGeneration.Engine.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network;
using System.Numerics;

namespace _3dTerrainGeneration.Game.GameWorld.Entities
{
    internal static class EntityTypeExtensions
    {
        public static EntityBase GetEntity(this EntityType entityType, World world, Vector3 pos, Vector3 motion, int id)
        {
            switch (entityType)
            {
                case EntityType.Player:
                    return new Player(world, id);
                case EntityType.BlueSlime:
                    return new BlueSlime(world, pos, id);
                case EntityType.Demon:
                    return new Demon(world, pos, id);
                case EntityType.Frog:
                    return new Frog(world, pos, id);
                case EntityType.Spider:
                    return new Spider(world, pos, id);
                case EntityType.FireBall:
                    return new FireBall(world, pos, motion, id);
                default:
                    return null;
            }
        }
    }
}
