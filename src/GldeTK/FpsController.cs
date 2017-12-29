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
        float motion_jumpImpulse = 3f;

        MouseState lastMouse = new MouseState();
        public MouseState LastMouseState => lastMouse;
        KeyboardState lastKeyboard = new KeyboardState();
        public KeyboardState LastKeyboardState => lastKeyboard;
        float yaw = 0.0f;
        float pitch = 0.0f;

        public Ray Update(float delta, Ray rayOrigin)
        {
            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            Ray motionStep = rayOrigin;

            UpdateMouse(mouse, delta, motionStep);
            UpdateKeyboard(keyboard, delta, motionStep);

            lastKeyboard = keyboard;
            lastMouse = mouse;

            return motionStep;
        }

        protected void UpdateKeyboard(KeyboardState keyboard, float delta, Ray nextStep)
        {
            nextStep.Origin = Vector3.Zero;

            float deltaStep = motion_Speed * delta;

            if (keyboard.IsKeyDown(Key.W))
                nextStep.Origin += nextStep.Target * deltaStep;

            if (keyboard.IsKeyDown(Key.S))
                nextStep.Origin -= nextStep.Target * deltaStep;

            if (keyboard.IsKeyDown(Key.A))
                nextStep.Origin -= Vector3.Normalize(Vector3.Cross(nextStep.Target, nextStep.Up)) * deltaStep;

            if (keyboard.IsKeyDown(Key.D))
                nextStep.Origin += Vector3.Normalize(Vector3.Cross(nextStep.Target, nextStep.Up)) * deltaStep;

            if (keyboard.IsKeyDown(Key.ShiftLeft))
                nextStep.Origin -= nextStep.Up * deltaStep;

            if (keyboard.IsKeyDown(Key.Space))
                nextStep.Origin += nextStep.Up * deltaStep * motion_jumpImpulse;
        }

        protected void UpdateMouse(MouseState mouse, float delta, Ray nextStep)
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
            nextStep.SetTarget(yaw, pitch);
        }
    }
}
