using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace GldeTK
{
    public class MainForm : GameWindow
    {
        const string APP_NAME = "GldeTK";
        const string FRAGMENT_FILENAME = "GldeTK.shaders.fragment.c";
        const string VERTEX_FILENAME = "GldeTK.shaders.vertex.c";
        const string GEOMETRY_FILENAME = "GldeTK.shaders.geometry.c";


        Vector3 camRo = new Vector3(0.0f, 1.0f, 0);
        Vector3 camTa = new Vector3(0, 0.0f, 0);

        int FULLSCREEN_W = 1920,
            FULLSCREEN_H = 1080;

        int h_vertex,
            h_fragment,
            h_shaderProgram;

        int uf_iGlobalTime,
            uf_iResolution,
            uf_CamRo,
            uf_CamTa;

        public MainForm()
        {
            Title = APP_NAME;
            VSync = VSyncMode.Adaptive;
            Width = 1024;
            Height = 768;
        }

        float lastX, lastY;
        float yaw;
        float pitch;

        float ToRadians(float degree)
        {
            return ((float)Math.PI / 180f) * degree;
        }

        Stopwatch stopwatch;
        protected override void OnLoad(EventArgs e)
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();

            CreateShaders();

            GL.Disable(EnableCap.DepthTest);
        }

        private int GetUniformLocation(string uniformName)
        {
            return GL.GetUniformLocation(h_shaderProgram, uniformName);
        }

        private void CreateShaders()
        {
            h_vertex = GL.CreateShader(ShaderType.VertexShader);
            h_fragment = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(h_vertex, LoadEmbeddedFile(VERTEX_FILENAME));
            GL.ShaderSource(h_fragment, LoadEmbeddedFile(FRAGMENT_FILENAME));

            GL.CompileShader(h_vertex);
            GL.CompileShader(h_fragment);

            h_shaderProgram = GL.CreateProgram();

            GL.AttachShader(h_shaderProgram, h_vertex);
            GL.AttachShader(h_shaderProgram, h_fragment);

            GL.LinkProgram(h_shaderProgram);
            GL.UseProgram(h_shaderProgram);

            GL.DeleteShader(h_vertex);      h_vertex = -1;
            GL.DeleteShader(h_fragment);    h_fragment = -1;

            uf_iGlobalTime = GetUniformLocation("iGlobalTime");
            uf_iResolution = GetUniformLocation("iResolution");
            uf_CamRo = GetUniformLocation("CamRo");
            uf_CamTa = GetUniformLocation("CamTa");
            //uf_iMouse = GetUniformLocation("iMouse");
        }

        const float PLAYER_MOVE_SPEED = .2f;
        Vector3 camFront = new Vector3(0.0f, 0.0f, -1.0f);
        Vector3 camUp = new Vector3(0.0f, 1.0f, 0.0f).Normalized();

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
        }

        float playerAcc = 0.0f;
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            UpdateKeyInput();
            OnMouseMove();
        }

        private bool IsKeyPressed(Key key)
        {
            KeyboardState state = Keyboard.GetState();
            return state.IsKeyUp(key) && lastState.IsKeyDown(key);
        }

        const float PLAYER_RADIUS = 1.0f;
        private void UpdatePlayerMove(KeyboardState state)
        {
            Vector3 moveDir = Vector3.Zero;

            if (state.IsKeyDown(Key.W))
                moveDir += camFront * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.S))
                moveDir -= camFront * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.A))
                moveDir -= Vector3.Normalize(Vector3.Cross(camFront, camUp)) * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.D))
                moveDir += Vector3.Normalize(Vector3.Cross(camFront, camUp)) * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.ShiftLeft))
                moveDir -= camUp * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.Space))
                moveDir += camUp * PLAYER_MOVE_SPEED;
            else
                playerAcc += PLAYER_MOVE_SPEED / 100;   // TODO should make gravity constant more phisical

            moveDir.Y -= playerAcc;  // gravity

            float d = Phys.CastRay(camRo, moveDir.Normalized(), PLAYER_RADIUS);

            if (d >= PLAYER_RADIUS * 1.1f)
                camRo += moveDir;
            else
            {   // collide here
                playerAcc = 0.0f;

                // smooth wall sliding
                Vector3 hitPoint = camRo + moveDir * PLAYER_RADIUS;
                Vector3 norm = Phys.CalcNormal(hitPoint);
                Vector3 invNorm = -norm;
                invNorm *= (moveDir * norm).LengthFast;

                camRo += moveDir - invNorm; // + wall sliding direction
            }
        }

        KeyboardState lastState = new KeyboardState();
        private void UpdateKeyInput()
        {
            KeyboardState state = Keyboard.GetState();

            UpdatePlayerMove(state);

            if (state.IsKeyDown(Key.Escape))
                Exit();

            if (IsKeyPressed(Key.F11))
            {
                if (WindowState == WindowState.Fullscreen)
                {
                    DisplayDevice
                        .GetDisplay(DisplayIndex.Default)
                        .RestoreResolution();

                    WindowState = WindowState.Normal;
                    CursorVisible = true;
                }
                else
                {
                    DisplayDevice
                        .GetDisplay(DisplayIndex.Default)
                        .ChangeResolution(FULLSCREEN_W, FULLSCREEN_H, 32, 60);

                    WindowState = WindowState.Fullscreen;
                    CursorVisible = false;
                }
            } // if state F11

            lastState = state;
        } // UpdateKeyInput()

        const float MOUSE_SENSITIVITY = .003f;
        const float PI = 3.14152f;
        const float PID2 = PI / 2f;

        void OnMouseMove()
        {
            MouseState ms = Mouse.GetState();

            yaw   += (ms.X - lastX) * MOUSE_SENSITIVITY;
            pitch += (lastY - ms.Y) * MOUSE_SENSITIVITY;

            if (pitch > PID2)
                pitch = PID2;
            else
                if (pitch < -PID2)
                    pitch = -PID2;

            camFront.X = (float)(Math.Cos(yaw) * Math.Cos(pitch));
            camFront.Y = (float)(Math.Sin(pitch));
            camFront.Z = (float)(Math.Sin(yaw) * Math.Cos(pitch));
            camFront.NormalizeFast();

            camTa = camFront + camRo;

            lastX = ms.X;
            lastY = ms.Y;
        }

        float iGlobalTime = 0;
        double delta = 0;
        double lastTime = 0;
        double s1_timer = 0;
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            delta = stopwatch.ElapsedTicks - lastTime;
            lastTime = stopwatch.ElapsedTicks;
            iGlobalTime = stopwatch.ElapsedMilliseconds * 0.001f;

            if (iGlobalTime - s1_timer > 1)
            {
                Title = $"{APP_NAME} — {(delta * 0.0001).ToString("0.")}ms, {(10000000 / delta).ToString("0")}fps";
                s1_timer = iGlobalTime;
            }

            GL.Uniform1(uf_iGlobalTime, iGlobalTime);
            GL.Uniform3(uf_iResolution, Width, Height, 0.0f);
            GL.Uniform3(uf_CamRo, camRo);
            GL.Uniform3(uf_CamTa, camTa);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            SwapBuffers();
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.DeleteProgram(h_shaderProgram);
            GL.DeleteShader(h_vertex);
            GL.DeleteShader(h_fragment);

            base.OnClosed(e);
        }

        private string LoadEmbeddedFile(string filename)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename);
            TextReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
