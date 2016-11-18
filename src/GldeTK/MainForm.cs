using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;
using System.Reflection;
using OpenTK.Graphics;
using System.Diagnostics;

namespace GldeTK
{
    public class MainForm : GameWindow
    {
        const string APP_NAME = "GldeTK";
        const string FRAGMENT_FILENAME = "GldeTK.shaders.fragment.c";
        const string VERTEX_FILENAME = "GldeTK.shaders.vertex.c";
        const string GEOMETRY_FILENAME = "GldeTK.shaders.geometry.c";

        int FULLSCREEN_W = 800,
            FULLSCREEN_H = 600;

        int h_vertex,
            h_fragment,
            h_shaderProgram;

        int uf_iGlobalTime,
            uf_iResolution,
            uf_PlayerPos,
            uf_iMouse;

        public MainForm()
        {
            Title = APP_NAME;
            KeyDown += GameForm_KeyDown;
            Mouse.Move += Mouse_Move;
            VSync = VSyncMode.On;
        }

        int m_xd = 0, m_yd = 0;
        private void Mouse_Move(object sender, MouseMoveEventArgs e)
        {
            //if (e.Mouse.IsButtonDown(MouseButton.Left))
            {
                m_xd = e.X;
                m_yd = e.Y;
            }
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
            uf_PlayerPos = GetUniformLocation("PlayerPos");
            uf_iMouse = GetUniformLocation("iMouse");
        }

        private void GameForm_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W:
                    break;
                case Key.Escape:
                    Exit();
                    break;
                case Key.F11:
                    if (WindowState == WindowState.Fullscreen)
                    {
                        DisplayDevice.GetDisplay(DisplayIndex.Default).RestoreResolution();
                        WindowState = WindowState.Normal;
                    }
                    else
                    {
                        DisplayDevice.GetDisplay(DisplayIndex.Default).ChangeResolution(FULLSCREEN_W, FULLSCREEN_H, 32, 60);
                        WindowState = WindowState.Fullscreen;
                    }
                    break;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
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
            //GL.Uniform3(uf_PlayerPos, 0, 0, 0.0f);
            GL.Uniform4(uf_iMouse, m_xd, m_yd, 0f, 0f);

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
