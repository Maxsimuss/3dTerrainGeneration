using _3dTerrainGeneration.Engine.World.Entity;
using System.Collections.Generic;

namespace _3dTerrainGeneration.Engine.GameWorld.Entity
{
    internal class EntityManager
    {
        Dictionary<int, EntityBase> entities = new Dictionary<int, EntityBase>();

        private static EntityManager instance;
        public static EntityManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EntityManager();
                }

                return instance;
            }
        }

        private EntityManager()
        {

        }

        public EntityBase AddEntity(EntityBase entity)
        {
            entities.Add(entity.EntityId, entity);

            return entity;
        }

        public int GetNextEntityId()
        {
            if (!entities.ContainsKey(entities.Count))
            {
                return entities.Count;
            }


            // may be slow
            int i = 0;
            while (entities.ContainsKey(i))
            {
                i++;
            }

            return i;
        }

        public void RemoveEntity(int id)
        {
            entities.Remove(id);
        }

        public void Tick()
        {
            foreach (var entity in entities.Values)
            {
                entity.Tick();
            }
        }

        public void Render()
        {
            foreach (var entity in entities.Values)
            {
                (entity as IDrawableEntity)?.Render();
            }
        }
    }
}
