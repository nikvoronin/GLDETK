using OpenTK;

namespace GldeTK
{
    public class RayUp : Ray
    {
        protected Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
        public virtual Vector3 Up
        {
            get => up;
            set => up = value;
        }
    }
}
