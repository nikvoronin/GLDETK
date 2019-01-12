﻿using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;
using System.Reflection;


namespace GldeTK
{
    public class Render
    {
        int h_vertex,
            h_fragment,
            h_shaderProgram;

        int uf_iGlobalTime,
            uf_iResolution,
            uf_CamRo,
            um3_CamProj,
            ubo_GlobalMap;

        private int GetUniformLocation(string uniformName)
        {
            return GL.GetUniformLocation(h_shaderProgram, uniformName);
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

        private string LoadEmbeddedFile(string filename)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename);
            TextReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        private void CreateShaders()
        {
            h_vertex = GL.CreateShader(ShaderType.VertexShader);
            h_fragment = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(h_vertex, LoadEmbeddedFile(Const.VERTEX_FILENAME));
            GL.ShaderSource(h_fragment, LoadEmbeddedFile(Const.FRAGMENT_FILENAME));

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

            uf_iGlobalTime = GetUniformLocation(Const.UF_TIMER);
            uf_iResolution = GetUniformLocation(Const.UF_RESOLUTION);
            uf_CamRo = GetUniformLocation(Const.UF_RAY_ORIGIN);
            um3_CamProj = GetUniformLocation(Const.UF_PROJECTION_MATRIX);
        }

        public void Start()
        {
            CreateShaders();
            GL.Disable(EnableCap.DepthTest);
        }

        private void CreateMapUbo()
        {
            int binding_point = 1;
            int block_index = GL.GetUniformBlockIndex(h_shaderProgram, Const.UBO_SDELEMENTSMAP_BLOCKNAME);
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

        internal void OnResize(int width, int height)
        {
            GL.Viewport(0, 0, width, height);
        }

        internal void OnFrame(float globalTime, int width, int height, Camera camera)
        {
            GL.UseProgram(h_shaderProgram);

            GL.Uniform1(uf_iGlobalTime, globalTime);
            GL.Uniform3(uf_iResolution, width, height, 0.0f);
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
        }

        internal void Stop()
        {
            GL.DeleteBuffers(1, ref ubo_GlobalMap);
            GL.DeleteProgram(h_shaderProgram);
            RemoveShader(h_shaderProgram, h_vertex);
            RemoveShader(h_shaderProgram, h_fragment);
        }
    }
}
