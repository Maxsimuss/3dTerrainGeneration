using System.Numerics;

namespace _3dTerrainGeneration.Engine.Physics
{
    internal class AxisAlignedBB
    {
        public Vector3 Position { get; private set; }
        public Vector3 Size { get; private set; }

        public AxisAlignedBB(AxisAlignedBBPrototype prototype)
        {
            Size = new Vector3(prototype.width, prototype.height, prototype.width);
        }

        public AxisAlignedBB(Vector3 position, Vector3 size)
        {
            Position = position;
            Size = size;
        }

        public void SetPositionCenteredXZ(Vector3 position)
        {
            Position = position - new Vector3(Size.X, 0, Size.Z) / 2;
        }

        //            if (boxCopy1.CheckCollision(boxCopy2, velocity* dir, out float collisionTime, out Vector2D collisionNormal))
        //    {
        //        box1.Position += velocity* collisionTime * .95f;
        //velocity *= collisionNormal;
        //    }
        //    else
        //    {
        //        box1.Position += velocity;
        //    }
        public static bool Check(AxisAlignedBB a, AxisAlignedBB b, Vector3 velocity, out float collisionTime, out Vector3 collisionNormal)
        {
            return a.CheckCollision(b, velocity, out collisionTime, out collisionNormal);
        }

        private bool CheckCollision(AxisAlignedBB other, Vector3 velocity, out float collisionTime, out Vector3 collisionNormal)
        {




            collisionTime = 1.0f;
            collisionNormal = new Vector3(0, 0, 0);
            return false;


            //float entryTime = Math.Max(entryTimeX, entryTimeZ);
            //float exitTime = Math.Min(exitTimeX, exitTimeZ);

            //if (entryTime > exitTime || entryTime < 0.0f || entryTime > 1.0f)
            //{
            //    collisionTime = 1.0f;
            //    collisionNormal = new Vector3(0, 0, 0);
            //    return false;
            //}

            //collisionTime = entryTime;

            //if (entryTimeX > entryTimeZ)
            //    collisionNormal = new Vector3(0, 1, 1);
            //else
            //    collisionNormal = new Vector3(1, 1, 0);

            ////if (entryTimeX > entryTimeY && entryTimeX > entryTimeZ)
            ////    collisionNormal = new Vector3(0, 1, 1);
            ////else if (entryTimeY > entryTimeZ)
            ////    collisionNormal = new Vector3(1, 0, 1);
            ////else
            ////    collisionNormal = new Vector3(1, 1, 0);

            //return true;
        }
    }
}
