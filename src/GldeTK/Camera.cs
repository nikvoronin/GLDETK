using OpenTK;
using System;

namespace GldeTK
{
    public class Camera
    {
        Vector3 origin = Vector3.Zero;
        Vector3 target = Vector3.Zero;
        Vector3 front;
        Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
        public Matrix3 Projection = Matrix3.Zero;

        public Vector3 Origin
        {
            get {
                return origin;
            }

            set {
                origin = value;
                front = Vector3.NormalizeFast((target - origin));
                Projection = GetProjection(origin, target, up);
            }
        }

        public Vector3 Target
        {
            get {
                return target;
            }

            set {
                target = value;
                front = (target - origin).Normalized();
                Projection = GetProjection(origin, target, up);
            }
        }

        /// <summary>
        /// Front direction
        /// </summary>
        public Vector3 Front
        {
            get {
                return front;
            }

            set {
                target = origin + value;
                Projection = GetProjection(origin, target, up);
            }
        }

        public Vector3 Up
        {
            get {
                return up;
            }

            set {
                up = value;
                Projection = GetProjection(origin, target, up);
            }
        }

        public Camera() { }

        public Camera(Vector3 origin, Vector3 target, Vector3 up)
        {
            Origin = origin;
            Target = target;
            Up = up;
        }

        /// <summary>
        /// Set new Front of the Camera. Angles must be in radians.
        /// </summary>
        public void SetFront(float yaw, float pitch)
        {
            Front =
                Vector3.NormalizeFast(
                    new Vector3(
                        (float)(Math.Cos(yaw) * Math.Cos(pitch)),
                        (float)(Math.Sin(pitch)),
                        (float)(Math.Sin(yaw) * Math.Cos(pitch))
                        ));
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
