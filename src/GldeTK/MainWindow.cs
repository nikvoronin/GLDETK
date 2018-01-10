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
        const string APP_NAME = "GldeTK";
        const string FRAGMENT_FILENAME = "GldeTK.shaders.fragment.c";
        const string VERTEX_FILENAME = "GldeTK.shaders.vertex.c";
        const string GEOMETRY_FILENAME = "GldeTK.shaders.geometry.c";
        const string RELEASE_DATE = "09 Jan 2018";
        const string UBO_SDELEMENTSMAP_BLOCKNAME = "SdElements";
        const int UBO_SDELEMENTSMAP_BLOCKCOUNT = 256;
        const float INPUT_UPDATE_INTERVAL = 10; // every ms

        Camera camera;
        FpsController fpsController;
        Physics phy;

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

        Timer timer;

        public MainWindow()
        {
            Title = APP_NAME;
            VSync = VSyncMode.Adaptive;
            Width = 1024;
            Height = 768;

            camera = new Camera(
                    new Vector3(0, 1, 0),
                    new Vector3(0, 0, -1),
                    new Vector3(0, 1, 0)
                    );

            phy = new Physics();
            fpsController = new FpsController();

            timer = new Timer(INPUT_UPDATE_INTERVAL);
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

            float sd = 0f;

            // gravity free fall
            Vector3 freeFallVector = phy.Gravity(
                delta,
                camera.RayCopy,
                player_hitRadius,
                motionStep.Origin.Y > 0
                );

            motionStep.Origin += freeFallVector;

            // wall collide
            sd = phy.CastRay(
                camera.Origin,
                Vector3.NormalizeFast(motionStep.Origin)
                );

            // when hit the wall
            if (sd <= player_hitRadius)
            {
                camera.Target = motionStep.Target;    // view only
                // smooth wall sliding
                Vector3 hitPoint = camera.Origin + motionStep.Origin * player_hitRadius;
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

            RemoveShader(h_shaderProgram, h_vertex);
            RemoveShader(h_shaderProgram, h_fragment);

            GL.UseProgram(h_shaderProgram);
            CreateMapUbo();

            uf_iGlobalTime = GetUniformLocation("iGlobalTime");
            uf_iResolution = GetUniformLocation("iResolution");
            uf_CamRo = GetUniformLocation("ro");
            um3_CamProj = GetUniformLocation("camProj");
        }

        /// <summary>
        /// Detach than delete shader
        /// </summary>
        /// <param name="h_program">Shader program name index</param>
        /// <param name="h_index">Shader name index</param>
        private void RemoveShader(int h_program, int h_index)
        {
            GL.DetachShader(h_program, h_index);
            GL.DeleteShader(h_index);
            h_index = -1;
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


        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
        }

        float player_hitRadius = 1.0f;   // TODO change later
        KeyboardState lastKeyboard = new KeyboardState();
        private void UpdateWindowKeys(KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Key.Escape))
                Exit();

            if (keyboard[Key.F11] && (lastKeyboard[Key.F11] != keyboard[Key.F11]))
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
        } // UpdateWindowKeys()

        double s1_timer = 0;    // smooth fps printing

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            float delta = (float)e.Time;

            if (phy.GlobalTime - s1_timer > 1)
            {
                Title = $"{APP_NAME}, {RELEASE_DATE} — {(delta * 1000).ToString("0.")}ms, {(1 / delta).ToString("0")}fps // {camera.Origin.X.ToString("0.0")} : {camera.Origin.Y.ToString("0.0")} : {camera.Origin.Z.ToString("0.0")} ";
                s1_timer = phy.GlobalTime;
            }

            GL.UseProgram(h_shaderProgram);

            GL.Uniform1(uf_iGlobalTime, phy.GlobalTime);
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
            timer.Enabled = false;

            GL.DeleteBuffers(1, ref ubo_GlobalMap);
            GL.DeleteProgram(h_shaderProgram);
            RemoveShader(h_shaderProgram, h_vertex);
            RemoveShader(h_shaderProgram, h_fragment);

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
