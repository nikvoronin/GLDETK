using OpenTK;

namespace GldeTK
{
    public class Camera
    {
        Vector3 origin = Vector3.Zero;
        Vector3 target = Vector3.Zero;
        Vector3 front;
        Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
        Matrix3 projection = Matrix3.Zero;

        public Vector3 Origin
        {
            get {
                return origin;
            }

            set {
                origin = value;
                front = (target - origin).Normalized();
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
            }
        }

        public Vector3 Up
        {
            get {
                return up;
            }

            set {
                up = value;
                projection = GetProjection(origin, target, up);
            }
        }

        public Camera() { }

        public Camera(Vector3 origin, Vector3 target, Vector3 up)
        {
            Origin = origin;
            Target = target;
            Up = up;
        }

        public Matrix3 Projection
        {
            get {
                return projection;
            }
        }

        public static Matrix3 GetProjection(Vector3 origin, Vector3 target, Vector3 up)
        {
            Vector3 cw = Vector3.Normalize(target - origin);
            Vector3 cu = Vector3.Normalize(Vector3.Cross(cw, up));
            Vector3 cv = Vector3.Normalize(Vector3.Cross(cu, cw));

            return
                new Matrix3(cu, cv, cw);
        }
    }
}
