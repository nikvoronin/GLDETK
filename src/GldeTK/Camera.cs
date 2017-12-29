using OpenTK;

namespace GldeTK
{
    public class Camera : Ray
    {
        public Matrix3 Projection = Matrix3.Zero;

        public override Vector3 Origin
        {
            set
            {
                base.Origin = value;
                UpdateFront();
                UpdateProjection();
            }
        }

        public override Vector3 Target
        {
            set
            {
                base.Target = value;
                UpdateFront();
                UpdateProjection();
            }
        }

        public override Vector3 Up
        {
            set
            {
                base.Up = value;
                UpdateFront();
                UpdateProjection();
            }
        }

        public Camera() { }

        public Camera(Vector3 origin, Vector3 target, Vector3 up)
        {
            this.origin = origin;
            this.target = target;
            this.up = up;
            UpdateFront();
            UpdateProjection();
        }

        public virtual Ray RayCopy => new Ray(origin, target, up);

        public override void SetTarget(float yaw, float pitch)
        {
            base.SetTarget(yaw, pitch);
            UpdateProjection();
        }

        /// <summary>
        /// Shift camera's origin to Origin+Step and update target and front
        /// </summary>
        /// <param name="step">Translate shift</param>
        public virtual void Translate(Ray ray)
        {
            origin += ray.Origin;
            target = origin + ray.Front;

            UpdateFront();
            UpdateProjection();
        }

        protected void UpdateProjection() => Projection = GetProjection(origin, target, up);

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
