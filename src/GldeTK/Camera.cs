using OpenTK;

namespace GldeTK
{
    public class Camera : RayUp
    {
        public Matrix3 Projection = Matrix3.Zero;

        public override Vector3 Origin
        {
            set {
                base.Origin = value;
                UpdateProjection();
            }
        }

        public override Vector3 Target
        {
            set
            {
                base.Target = value;
                UpdateProjection();
            }
        }

        public override Vector3 Up
        {
            set {
                base.Up = value;
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
        }

        public override void SetTarget(float yaw, float pitch)
        {
            base.SetTarget(yaw, pitch);
            UpdateProjection();
        }

        /// <summary>
        /// Set new origin of the Camera.
        /// </summary>
        /// <param name="origin">New camera's origin.</param>
        public virtual void MoveTo(Vector3 origin)
        {
            this.origin = origin;
            target = origin + front;
            UpdateProjection();
        }

        /// <summary>
        /// Shift camera's origin to Origin+Step and update target and front
        /// </summary>
        /// <param name="step">Translate shift</param>
        public virtual void Translate(Vector3 step)
        {
            origin += step;
            target = origin + front;
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
