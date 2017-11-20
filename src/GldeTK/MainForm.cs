using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;

namespace GldeTK
{
    public class MainForm : GameWindow
    {
        const string APP_NAME = "GldeTK";
        const string FRAGMENT_FILENAME = "GldeTK.shaders.fragment.c";
        const string VERTEX_FILENAME = "GldeTK.shaders.vertex.c";
        const string GEOMETRY_FILENAME = "GldeTK.shaders.geometry.c";


        Vector3 camRo = new Vector3(0, 1.0f, 0);
        Vector3 camTa = new Vector3(1, 1.0f, 1);

        int FULLSCREEN_W = 800,
            FULLSCREEN_H = 600;

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
            VSync = VSyncMode.On;

            camTa.Normalize();
        }

        bool firstMouse = false;
        float lastX, lastY;
        float yaw = -90.0f;
        float pitch = 0.0f;

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
        Vector3 camUp = new Vector3(0.0f, 1.0f, 0.0f);

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            UpdateKeyInput();
            OnMouseMove();
        }

        private void UpdateKeyInput()
        {
            KeyboardState state = Keyboard.GetState();

            if(state.IsKeyDown(Key.W))
                camRo += camFront * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.S))
                camRo -= camFront * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.A))            
                camRo -= Vector3.Normalize(Vector3.Cross(camFront, camUp)) * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.D))
                camRo += Vector3.Normalize(Vector3.Cross(camFront, camUp)) * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.ShiftLeft))
                camRo -= Vector3.Normalize(camUp) * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.Space))
                camRo += Vector3.Normalize(camUp) * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.Escape))
                Exit();

            if (state.IsKeyDown(Key.F11))
            {
                if (WindowState == WindowState.Fullscreen)
                {
                    DisplayDevice.GetDisplay(DisplayIndex.Default).RestoreResolution();
                    WindowState = WindowState.Normal;
                    CursorVisible = true;
                }
                else
                {
                    DisplayDevice.GetDisplay(DisplayIndex.Default).ChangeResolution(FULLSCREEN_W, FULLSCREEN_H, 32, 60);
                    WindowState = WindowState.Fullscreen;
                    CursorVisible = false;
                }
            } // if state F11
        } // UpdateKeyInput()

        Point mpoint = Point.Empty;
        void OnMouseMove()
        {
            MouseState ms = Mouse.GetState();
            mpoint.X = ms.X;
            mpoint.Y = ms.Y;

            mpoint = PointToClient(mpoint);
            float xpos = mpoint.X;
            float ypos = mpoint.Y;

            if (firstMouse)
            {
                lastX = xpos;
                lastY = ypos;
                firstMouse = false;
            }

            float xoffset = xpos - lastX;
            float yoffset = lastY - ypos;

            lastX = xpos;
            lastY = ypos;

            float sensitivity = 0.7f;
            xoffset *= sensitivity;
            yoffset *= sensitivity;

            yaw += xoffset;
            pitch += yoffset;

            if (pitch > 89.0f)
                pitch = 89.0f;
            if (pitch < -89.0f)
                pitch = -89.0f;

            Vector3 front = new Vector3();
            front.X = (float)(Math.Cos(ToRadians(yaw)) * Math.Cos(ToRadians(pitch)));
            front.Y = (float)(Math.Sin(ToRadians(pitch)));
            front.Z = (float)(Math.Sin(ToRadians(yaw)) * Math.Cos(ToRadians(pitch)));
            camFront = Vector3.Normalize(front);
            camTa = camFront + camRo;

            
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
            //GL.Uniform3(uf_CamRo, p_pos);
            //GL.Uniform4(uf_iMouse, m_xd, m_yd, 0f, 0f);

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
