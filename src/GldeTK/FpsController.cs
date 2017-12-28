using OpenTK;
using OpenTK.Input;

namespace GldeTK
{
    public class FpsController
    {
        public FpsController() { }

        /// <summary>
        /// 0.1 is slower than 0.9
        /// </summary>
        float mouse_sensitivity = 0.15f;

        /// <summary>
        /// From 1 to 100. 15 by default
        /// </summary>
        public float MouseSensitivity
        {
            get => mouse_sensitivity * 100f;
            set => mouse_sensitivity = value / 100f;
        }

        float motion_Speed = 5f;
        float motion_fallSpeed = .0f;
        float player_hitRadius = 1.0f;   // TODO change later

        MouseState lastMouse = new MouseState();
        public MouseState LastMouseState => lastMouse;
        KeyboardState lastKeyboard = new KeyboardState();
        public KeyboardState LastKeyboardState => lastKeyboard;
        float yaw = 0.0f;
        float pitch = 0.0f;

        public void Update(float delta, Camera camera)
        {
            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            UpdateKeyboard(keyboard, delta, camera);
            UpdateMouse(mouse, delta, camera);

            lastKeyboard = keyboard;
            lastMouse = mouse;
        }

        protected void UpdateKeyboard(KeyboardState keyboard, float delta, Camera camera)
        {
            Vector3 nextStep = Vector3.Zero;

            float deltaStep = motion_Speed * delta;

            if (keyboard.IsKeyDown(Key.W))
                nextStep += camera.Front * deltaStep;

            if (keyboard.IsKeyDown(Key.S))
                nextStep -= camera.Front * deltaStep;

            if (keyboard.IsKeyDown(Key.A))
                nextStep -= Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up)) * deltaStep;

            if (keyboard.IsKeyDown(Key.D))
                nextStep += Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up)) * deltaStep;

            if (keyboard.IsKeyDown(Key.ShiftLeft))
                nextStep -= camera.Up * deltaStep;

            if (keyboard.IsKeyDown(Key.Space))
            {
                motion_fallSpeed = 0.0f;
                nextStep += camera.Up * deltaStep;
            }
            else
            {
                motion_fallSpeed += 9.8f * delta;
                nextStep -= camera.Up * motion_fallSpeed * delta;  // gravity
            }

            float d = Phys.CastRay(camera.Origin, nextStep.Normalized(), player_hitRadius);

            if (d > player_hitRadius)
                camera.Translate(nextStep);
            else
            {   // collide here
                motion_fallSpeed = 0.0f;

                // smooth wall sliding
                Vector3 hitPoint = camera.Origin + nextStep * player_hitRadius;
                Vector3 norm = Phys.CalcNormal(hitPoint);
                Vector3 invNorm = -norm;
                invNorm *= (nextStep * norm).LengthFast;

                camera.Translate(nextStep - invNorm); // camRo + wall sliding direction
            }
        }

        protected void UpdateMouse(MouseState mouse, float delta, Camera camera)
        {
            int deltaX = mouse.X - lastMouse.X;
            int deltaY = lastMouse.Y - mouse.Y;

            if ((deltaX == 0) && (deltaY == 0))
                return;

            yaw += deltaX * mouse_sensitivity * delta;
            pitch += deltaY * mouse_sensitivity * delta;

            if (pitch > MathHelper.PiOver2)
                pitch = MathHelper.PiOver2;
            else
                if (pitch < -MathHelper.PiOver2)
                pitch = -MathHelper.PiOver2;

            /// Sets new Front of the Camera. Angles must be in radians.
            camera.SetTarget(yaw, pitch);
        }
    }
}
