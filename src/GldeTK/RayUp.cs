using OpenTK;

namespace GldeTK
{
    public class RayUp : Ray
    {
        protected Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);

        public RayUp() { }

        public RayUp(Vector3 origin, Vector3 target, Vector3 up) : base(origin, target)
        {
            this.up = up;
        }

        public virtual Vector3 Up
        {
            get => up;
            set => up = value;
        }
    }
}
