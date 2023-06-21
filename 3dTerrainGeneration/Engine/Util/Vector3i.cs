using System;
using System.Runtime.CompilerServices;

namespace _3dTerrainGeneration.Engine.Util
{
    public struct Vector3I
    {
        public int X, Y, Z;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public Vector3I(int X, int Y, int Z)
        {
            this.X = X; this.Y = Y; this.Z = Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool operator ==(Vector3I left, Vector3I right)
        {
            return left.X == right.X && left.Y == right.Y && left.Z == right.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool operator !=(Vector3I left, Vector3I right)
        {
            return left.X != right.X || left.Y != right.Y || left.Z != right.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static Vector3I operator +(Vector3I left, Vector3I right)
        {
            return new Vector3I(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static Vector3I operator -(Vector3I left, Vector3I right)
        {
            return new Vector3I(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static Vector3I operator /(Vector3I left, int right)
        {
            return new Vector3I(left.X / right, left.Y / right, left.Z / right);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static Vector3I operator *(Vector3I left, int right)
        {
            return new Vector3I(left.X * right, left.Y * right, left.Z * right);
        }

        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
                    2 => Z,
                    _ => throw new IndexOutOfRangeException("You tried to access this vector at index: " + index),
                };
            }
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            set
            {
                switch (index)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
                        Z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
                }
            }
        }

        public int LengthSq()
        {
            return X * X + Y * Y + Z * Z;
        }

        public float Length()
        {
            return MathF.Sqrt(X * X + Y * Y + Z * Z);
        }
    }

}
