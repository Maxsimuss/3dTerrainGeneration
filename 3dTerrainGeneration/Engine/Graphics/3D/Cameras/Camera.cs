using System;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Graphics._3D
{

    // from https://github.com/opentk/LearnOpenTK/blob/master/Common/Camera.cs
    public class Camera
    {
        // Those vectors are directions pointing outwards from the camera to define how it rotated
        private Vector3 _front = -Vector3.UnitZ;

        private Vector3 _up = Vector3.UnitY;

        private Vector3 _right = Vector3.UnitX;

        // Rotation around the X axis (radians)
        private float _pitch;

        // Rotation around the Y axis (radians)
        private float _yaw = -OpenTK.Mathematics.MathHelper.PiOver2; // Without this you would be started rotated 90 degrees right

        // The field of view of the camera (radians)
        private float _fov = OpenTK.Mathematics.MathHelper.PiOver2;

        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
        }

        // The position of the camera
        public Vector3 Position;
        public Vector3 Velocity;

        public float AspectRatio { get; set; }

        public Vector3 Front
        {
            get
            {
                return Vector3.Normalize(new Vector3(_front.X, _front.Y / AspectRatio, _front.Z));
            }
        }

        public Vector3 Up => _up;

        public Vector3 Right => _right;

        // We convert from degrees to radians as soon as the property is set to improve performance
        public float Pitch
        {
            get => OpenTK.Mathematics.MathHelper.RadiansToDegrees(_pitch);
            set
            {
                // We clamp the pitch value between -89 and 89 to prevent the camera from going upside down, and a bunch
                // of weird "bugs" when you are using euler angles for rotation.
                // If you want to read more about this you can try researching a topic called gimbal lock
                var angle = OpenTK.Mathematics.MathHelper.Clamp(value, -89.999f, 89.999f);
                _pitch = OpenTK.Mathematics.MathHelper.DegreesToRadians(angle);
                UpdateVectors();
            }
        }

        // We convert from degrees to radians as soon as the property is set to improve performance
        public float Yaw
        {
            get => OpenTK.Mathematics.MathHelper.RadiansToDegrees(_yaw);
            set
            {
                _yaw = OpenTK.Mathematics.MathHelper.DegreesToRadians(value);
                UpdateVectors();
            }
        }

        // The field of view (FOV) is the vertical angle of the camera view, this has been discussed more in depth in a
        // previous tutorial, but in this tutorial you have also learned how we can use this to simulate a zoom feature.
        // We convert from degrees to radians as soon as the property is set to improve performance
        public float Fov
        {
            get => OpenTK.Mathematics.MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = OpenTK.Mathematics.MathHelper.Clamp(value, 30f, 179f);
                _fov = OpenTK.Mathematics.MathHelper.DegreesToRadians(angle);
            }
        }

        // Get the view matrix using the amazing LookAt function described more in depth on the web tutorials
        public Matrix4x4 GetViewMatrix()
        {
            return Matrix4x4.CreateLookAt(Position, Position + _front, _up);
        }

        // Get the projection matrix using the same method we have used up until this point
        public Matrix4x4 GetProjectionMatrix()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(_fov, AspectRatio, .5f, 3072);
        }

        // This function is going to update the direction vertices using some of the math learned in the web tutorials
        private void UpdateVectors()
        {
            // First the front matrix is calculated using some basic trigonometry
            _front.X = (float)Math.Cos(_pitch) * (float)Math.Cos(_yaw);
            _front.Y = (float)Math.Sin(_pitch);
            _front.Z = (float)Math.Cos(_pitch) * (float)Math.Sin(_yaw);

            // We need to make sure the vectors are all normalized, as otherwise we would get some funky results
            _front = Vector3.Normalize(_front);

            // Calculate both the right and the up vector using cross product
            // Note that we are calculating the right from the global up, this behaviour might
            // not be what you need for all cameras so keep this in mind if you do not want a FPS camera
            _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
            _up = Vector3.Normalize(Vector3.Cross(_right, _front));
        }
    }
}