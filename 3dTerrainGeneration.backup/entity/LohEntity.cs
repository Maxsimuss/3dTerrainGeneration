using _3dTerrainGeneration.world;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.entity
{
    class LohEntity : DrawableEntity
    {
        public LohEntity(World world, MeshGenerator meshGenerator, int Buffer, Vector3 position) : base(world, meshGenerator, Buffer)
        {
            x = position.X; y = position.Y; z = position.Z; 
        }

        public LohEntity(World world, MeshGenerator meshGen, int Buffer, object p) : base(world, meshGen, Buffer)
        {
            this.p = p;
        }

        double updateTime = 0;
        double attackCoolDown = 0;
        Random random = new Random();
        private object p;

        public override void PhisycsUpdate(double fT)
        {
            base.PhisycsUpdate(fT);

            //Jump(false);

            updateTime += fT;
            if(updateTime > 1)
            {
                
                yaw = -Math.Atan2(x - world.player.x, z - world.player.z) / Math.PI * 180 - 90;
                updateTime = 0;
            }

            if((GetPosition() - world.player.GetPosition()).LengthSquared > 4)
            {
                MoveFacing(0, 20);
            } 
            else if((attackCoolDown -= fT) <= 0)
            {
                attackCoolDown = 1;
                world.player.Hurt(1);
            }
        }
    }
}
