using OpenTK;
using System;

namespace GldeTK
{
    public class Ray
    {
        protected Vector3 origin = Vector3.Zero;
        protected Vector3 target = Vector3.Zero;
        protected Vector3 front = Vector3.Zero;
        protected Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);

        public virtual Vector3 Origin
        {
            get => origin;
            set => origin = value;
        }

        public virtual Vector3 Target
        {
            get => target;
            set => target = value;
        }

        public virtual Vector3 Up
        {
            get => up;
            set => up = value;
        }

        /// <summary>
        /// Front direction of the Ray. Always normalized
        /// </summary>
        public Vector3 Front => front;

        public Ray() { }

        public Ray(Vector3 origin, Vector3 target)
        {
            this.origin = origin;
            this.target = target;
            UpdateFront();
        }

        public Ray(Vector3 origin, Vector3 target, Vector3 up)
        {
            this.origin = origin;
            this.target = target;
            this.up = up;
            UpdateFront();
        }

        protected void UpdateFront() => front = Vector3.NormalizeFast(target - origin);

        public virtual void SetTarget(float yaw, float pitch)
        {
            front =
                Vector3.NormalizeFast(
                    new Vector3(
                        (float)(Math.Cos(yaw) * Math.Cos(pitch)),
                        (float)(Math.Sin(pitch)),
                        (float)(Math.Sin(yaw) * Math.Cos(pitch))
                        ));

            target = origin + front;
        }

    }
}
