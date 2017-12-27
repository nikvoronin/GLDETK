using System;
using OpenTK;
using OpenTK.Input;

namespace GldeTK
{
    public class FpsController
    {
        public FpsController(Camera activeCamera)
        {
            ActiveCamera = activeCamera;
            //mouselook_front = Vector3.NormalizeFast(camera.Target - camera.Origin);
        }

        /// <summary>
        /// .001 is slower than .009
        /// </summary>
        float mouse_sensitivity = .003f;

        /// <summary>
        /// From 1 to 100. 30 by default
        /// </summary>
        public float MouseSensitivity
        {
            get => mouse_sensitivity * 10000f;
            set => mouse_sensitivity = value / 10000f;
        }

        float motion_stepSize = .2f;
        float motion_currentAcceleration = .0f;
        float player_hitRadius = 1.0f;   // TODO change later

        //Vector3 mouselook_front;
        private Camera camera = null;
        public Camera ActiveCamera
        {
            set => camera = value;
            get => camera;
        }

        MouseState lastMouse = new MouseState();
        public MouseState LastMouseState => lastMouse;
        KeyboardState lastKeyboard = new KeyboardState();
        public KeyboardState LastKeyboardState => lastKeyboard;
        float yaw = 0.0f;
        float pitch = 0.0f;

        public void Update()
        {
            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            UpdateKeyboard(keyboard);
            UpdateMouse(mouse);

            lastKeyboard = keyboard;
            lastMouse = mouse;
        }

        protected void UpdateKeyboard(KeyboardState keyboard)
        {
            Vector3 moveStep = Vector3.Zero;

            if (keyboard.IsKeyDown(Key.W))
                moveStep += camera.FrontDirection * motion_stepSize;

            if (keyboard.IsKeyDown(Key.S))
                moveStep -= camera.FrontDirection * motion_stepSize;

            if (keyboard.IsKeyDown(Key.A))
                moveStep -= Vector3.Normalize(Vector3.Cross(camera.FrontDirection, camera.Up)) * motion_stepSize;

            if (keyboard.IsKeyDown(Key.D))
                moveStep += Vector3.Normalize(Vector3.Cross(camera.FrontDirection, camera.Up)) * motion_stepSize;

            if (keyboard.IsKeyDown(Key.ShiftLeft))
                moveStep -= camera.Up * motion_stepSize;

            if (keyboard.IsKeyDown(Key.Space))
            {
                motion_currentAcceleration = 0.0f;
                moveStep += camera.Up * motion_stepSize;
            }
            else
                motion_currentAcceleration += motion_stepSize / 50;   // TODO should make gravity constant more phisical

            moveStep.Y -= motion_currentAcceleration;  // gravity

            float d = Phys.CastRay(camera.Origin, moveStep.Normalized(), player_hitRadius);

            if (d > player_hitRadius)
                camera.Origin += moveStep;
            else
            {   // collide here
                motion_currentAcceleration = 0.0f;

                // smooth wall sliding
                Vector3 hitPoint = camera.Origin + moveStep * player_hitRadius;
                Vector3 norm = Phys.CalcNormal(hitPoint);
                Vector3 invNorm = -norm;
                invNorm *= (moveStep * norm).LengthFast;

                camera.Origin += moveStep - invNorm; // camRo + wall sliding direction
            }
        }

        protected void UpdateMouse(MouseState mouse)
        {
            int deltaX = mouse.X - lastMouse.X;
            int deltaY = lastMouse.Y - mouse.Y;

            if ((deltaX == 0) && (deltaY == 0))
                return;

            yaw += deltaX * mouse_sensitivity;
            pitch += deltaY * mouse_sensitivity;

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
