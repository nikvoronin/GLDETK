using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;

namespace GldeTK
{
    public class MainWindow : GameWindow
    {
        Camera camera;
        FpsController fpsController;
        Physics phy;
        Render render;

        Timer timer;

        public MainWindow()
        {
            Title = Const.APP_NAME;
            VSync = VSyncMode.Adaptive;
            Width = Const.DISPLAY_XGA_W;
            Height = Const.DISPLAY_XGA_H;

            render = new Render();

            camera = new Camera(
                    new Vector3(3, 1, 0),
                    new Vector3(-1, 0, 0),
                    new Vector3(0, 1, 0)
                    );

            phy = new Physics();
            fpsController = new FpsController();

            timer = new Timer(Const.INPUT_UPDATE_INTERVAL);
            timer.Elapsed += Timer_Elapsed;
            lastTicks = DateTime.Now.Ticks;
            timer.Enabled = true;
        }

        long lastTicks;
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            float delta = (e.SignalTime.Ticks - lastTicks) / 10000000.0f;
            lastTicks = e.SignalTime.Ticks;
            phy.GlobalTime += delta;

            // update player input (keyboard_wasd+space+shift + mouse-look)
            Ray motionStep = fpsController.Update(delta, camera.RayCopy);

            // gravity free fall
            Vector3 freeFallVector = phy.Gravity(
                delta,
                camera.RayCopy,
                Const.PLAYER_HIT_RADIUS,
                motionStep.Origin.Y > 0
                );

            motionStep.Origin += freeFallVector;

            // wall collide
            float sd = phy.CastRay(
                camera.Origin,
                Vector3.NormalizeFast(motionStep.Origin)
                );

            // when hit the wall
            if (sd <= Const.PLAYER_HIT_RADIUS)
            {
                camera.Target = motionStep.Target;    // view only
                // smooth wall sliding
                Vector3 hitPoint = camera.Origin + motionStep.Origin * Const.PLAYER_HIT_RADIUS;
                Vector3 norm = phy.GetSurfaceNormal(hitPoint);
                Vector3 invNorm = -norm;
                invNorm *= (motionStep.Origin * norm).LengthFast;

                motionStep.Origin -= invNorm;
            }

            camera.Translate(motionStep);

            var keyboard = Keyboard.GetState();
            UpdateWindowKeys(keyboard);
            lastKeyboard = keyboard;
        }

        protected override void OnLoad(EventArgs e)
        {
            render.Start();
        }

        protected override void OnResize(EventArgs e)
        {
            render.OnResize(Width, Height);            
        }

        KeyboardState lastKeyboard = new KeyboardState();
        private void UpdateWindowKeys(KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Key.Escape))
                Exit();

            if (keyboard[Key.F11] && (lastKeyboard[Key.F11] != keyboard[Key.F11]))
            {
                DisplayDevice defaultDisplayDevice = DisplayDevice.GetDisplay(DisplayIndex.Default);

                if (WindowState == WindowState.Fullscreen)
                {
                    WindowState = WindowState.Normal;
                    CursorVisible = true;
                    defaultDisplayDevice.RestoreResolution();
                }
                else
                {
                    WindowState = WindowState.Fullscreen;
                    CursorVisible = false;
                    defaultDisplayDevice
                        .ChangeResolution(
                            Const.DISPLAY_FULLHD_W, Const.DISPLAY_FULLHD_H,
                            Const.DISPLAY_BITPERPIXEL,
                            Const.DISPLAY_REFRESH_RATE);
                }
            } // if state F11
        } // UpdateWindowKeys()

        double s1_timer = 0;    // smooth fps printing

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            float delta = (float)e.Time;

            if (phy.GlobalTime - s1_timer > 1)
            {
                Title = $"{Const.APP_NAME}, {Const.RELEASE_DATE} — {(delta * 1000).ToString("0.")}ms, {(1.0 / delta).ToString("0")}fps // {camera.Origin.X.ToString("0.0")} : {camera.Origin.Y.ToString("0.0")} : {camera.Origin.Z.ToString("0.0")} ";
                s1_timer = phy.GlobalTime;
            }

            render.OnFrame(phy.GlobalTime, Width, Height, camera);

            SwapBuffers();
        }

        protected override void OnUnload(EventArgs e)
        {
            timer.Enabled = false;

            render.Stop();

            base.OnClosed(e);
        }
    }
}
