using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GldeTK
{
    public class MainForm : GameWindow
    {
        const string APP_NAME = "GldeTK";
        const string FRAGMENT_FILENAME = "GldeTK.shaders.fragment.c";
        const string VERTEX_FILENAME = "GldeTK.shaders.vertex.c";
        const string GEOMETRY_FILENAME = "GldeTK.shaders.geometry.c";
        const string RELEASE_DATE = "2 Dec 2017";
        const string UBO_SDELEMENTSMAP_BLOCKNAME = "SdElements";
        const int UBO_SDELEMENTSMAP_BLOCKCOUNT = 256;

        Camera camera;

        int FULLSCREEN_W = 1920,
            FULLSCREEN_H = 1080;

        int h_vertex,
            h_fragment,
            h_shaderProgram;

        int uf_iGlobalTime,
            uf_iResolution,
            uf_CamRo,
            um3_CamProj,
            ubo_GlobalMap;

        public MainForm()
        {
            Title = APP_NAME;
            VSync = VSyncMode.Adaptive;
            Width = 1024;
            Height = 768;

            camera =
                new Camera(
                    new Vector3(0.0f, 1.0f, 0.0f),
                    new Vector3(0.0f, 1.0f, -1.0f),
                    new Vector3(0.0f, 1.0f, 0.0f)
                    );
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

        [StructLayout(LayoutKind.Explicit)]
        struct GlobalMapStruct
        {
            [FieldOffset(0)]
            public float[] GlobalMap;

            public static readonly int Size =
                    BlittableValueType<GlobalMapStruct>.Stride;
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

            GL.DetachShader(h_shaderProgram, h_vertex);
            GL.DetachShader(h_shaderProgram, h_fragment);
            GL.DeleteShader(h_vertex);      h_vertex = -1;
            GL.DeleteShader(h_fragment);    h_fragment = -1;

            GL.UseProgram(h_shaderProgram);
            CreateMapUbo();

            uf_iGlobalTime = GetUniformLocation("iGlobalTime");
            uf_iResolution = GetUniformLocation("iResolution");
            uf_CamRo = GetUniformLocation("ro");
            um3_CamProj = GetUniformLocation("camProj");
        }

        private void CreateMapUbo()
        {
            int binding_point = 1;
            int block_index = GL.GetUniformBlockIndex(h_shaderProgram, UBO_SDELEMENTSMAP_BLOCKNAME);
            GL.UniformBlockBinding(h_shaderProgram, block_index, binding_point);

            // Allocate space for the buffer
            GL.GetActiveUniformBlock(
                h_shaderProgram,
                block_index,
                ActiveUniformBlockParameter.UniformBlockDataSize,
                out int mapBlockSize);

            #region // Indexes and offsets of each block variable
            //// Query for the offsets of each block variable
            //var names =
            //    new[] {
            //        "GlobalMap.sdElements"
            //    };

            //var indices = new int[names.Length];
            //GL.GetUniformIndices(
            //    h_shaderProgram,
            //    names.Length,
            //    names,
            //    indices);

            //var offset = new int[names.Length];
            //GL.GetActiveUniforms(
            //    h_shaderProgram,
            //    names.Length,
            //    indices,
            //    ActiveUniformParameter.UniformOffset,
            //    offset);
            #endregion

            // Create the buffer object and copy the data
            GL.GenBuffers(1, out ubo_GlobalMap);
            GL.BindBuffer(BufferTarget.UniformBuffer, ubo_GlobalMap);

            GL.BufferData(
                BufferTarget.UniformBuffer,
                mapBlockSize,
                (IntPtr)null,
                BufferUsageHint.StreamDraw);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, binding_point, ubo_GlobalMap);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        const float PLAYER_MOVE_SPEED = .2f;

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
            Vector3 moveStep = Vector3.Zero;

            if (state.IsKeyDown(Key.W))
                moveStep += camera.Front * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.S))
                moveStep -= camera.Front * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.A))
                moveStep -= Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up)) * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.D))
                moveStep += Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up)) * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.ShiftLeft))
                moveStep -= camera.Up * PLAYER_MOVE_SPEED;

            if (state.IsKeyDown(Key.Space))
            {
                playerAcc = 0.0f;
                moveStep += camera.Up * PLAYER_MOVE_SPEED;
            }
            else
                playerAcc += PLAYER_MOVE_SPEED / 50;   // TODO should make gravity constant more phisical

            moveStep.Y -= playerAcc;  // gravity

            float d = Phys.CastRay(camera.Origin, moveStep.Normalized(), PLAYER_RADIUS);

            if (d > PLAYER_RADIUS)
                camera.Origin += moveStep;
            else
            {   // collide here
                playerAcc = 0.0f;

                // smooth wall sliding
                Vector3 hitPoint = camera.Origin + moveStep * PLAYER_RADIUS;
                Vector3 norm = Phys.CalcNormal(hitPoint);
                Vector3 invNorm = -norm;
                invNorm *= (moveStep * norm).LengthFast;

                camera.Origin += moveStep - invNorm; // camRo + wall sliding direction
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

            camera.SetFront(yaw, pitch);

            lastX = ms.X;
            lastY = ms.Y;
        }

        float iGlobalTime = 0;
        double delta = 0;
        double lastTime = 0;
        double s1_timer = 0;

        void DoTimers()
        {
            delta = stopwatch.ElapsedTicks - lastTime;
            lastTime = stopwatch.ElapsedTicks;
            iGlobalTime = stopwatch.ElapsedMilliseconds * 0.001f;

            if (iGlobalTime - s1_timer > 1)
            {
                Title = $"{APP_NAME} // {RELEASE_DATE} — {(delta * 0.0001).ToString("0.")}ms, {(10000000 / delta).ToString("0")}fps // {camera.Origin.X.ToString("0.0")} : {camera.Origin.Y.ToString("0.0")} : {camera.Origin.Z.ToString("0.0")} ";
                s1_timer = iGlobalTime;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            DoTimers();

            GL.UseProgram(h_shaderProgram);

            GL.Uniform1(uf_iGlobalTime, iGlobalTime);
            GL.Uniform3(uf_iResolution, Width, Height, 0.0f);
            GL.Uniform3(uf_CamRo, camera.Origin);
            GL.UniformMatrix3(um3_CamProj, false, ref camera.Projection);

            GL.BindBuffer(BufferTarget.UniformBuffer, ubo_GlobalMap);
            Vector4[] v = new Vector4[1];
            v[0] = new Vector4(1, 1, 2, 1);

            GL.BufferSubData<Vector4>(
                BufferTarget.UniformBuffer,
                (IntPtr)0,
                16,
                v
                );

            //float[] v = new float[4] { 1, 0, 0, 0 };

            //GL.BufferSubData<float>(
            //    BufferTarget.UniformBuffer,
            //    (IntPtr)0,
            //    16,
            //    v
            //    );

            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            GL.UseProgram(0);
            SwapBuffers();
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.DeleteBuffers(1, ref ubo_GlobalMap);
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
