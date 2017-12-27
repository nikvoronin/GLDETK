using OpenTK;
using System;

namespace GldeTK
{
    public class Camera
    {
        Vector3 origin = Vector3.Zero;
        Vector3 target = Vector3.Zero;
        Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
        Vector3 front = Vector3.Zero;
        public Matrix3 Projection = Matrix3.Zero;

        public Vector3 Origin
        {
            get => origin;

            set {
                origin = value;
                target = origin + front;
                Projection = GetProjection(origin, target, up);
            }
        }

        public Vector3 Target
        {
            get => target;

            set
            {
                target = value;
                front = Vector3.NormalizeFast(target - origin);
                Projection = GetProjection(origin, target, up);
            }
        }

        public Vector3 Up
        {
            get => up;

            set {
                up = value;
                Projection = GetProjection(origin, target, up);
            }
        }

        public Vector3 FrontDirection => front;

        public Camera() { }

        public Camera(Vector3 origin, Vector3 target, Vector3 up)
        {
            this.origin = origin;
            this.target = target;
            this.up = up;
            front = Vector3.NormalizeFast(target - origin);
        }

        public void SetTarget(float yaw, float pitch)
        {
            front =
                Vector3.NormalizeFast(
                    new Vector3(
                        (float)(Math.Cos(yaw) * Math.Cos(pitch)),
                        (float)(Math.Sin(pitch)),
                        (float)(Math.Sin(yaw) * Math.Cos(pitch))
                        ));

            target = origin + front;
            Projection = GetProjection(origin, target, up);
        }

        public static Matrix3 GetProjection(Vector3 origin, Vector3 target, Vector3 up)
        {
            Vector3 cw = Vector3.NormalizeFast(target - origin);
            Vector3 cu = Vector3.NormalizeFast(Vector3.Cross(cw, up));
            Vector3 cv = Vector3.NormalizeFast(Vector3.Cross(cu, cw));

            return
                new Matrix3(cu, cv, cw);
        }
    }
}
