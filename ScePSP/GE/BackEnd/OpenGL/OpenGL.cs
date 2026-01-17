using LightGL;
using ScePSP.GE;
using ScePSP.GE.State;
using ScePSP.Memory;
using ScePSP.Types;
using ScePSPUtils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace ScePSP.BackEnd.OpenGL
{
    public unsafe partial class GLBackEnd : GEBackEnd
    {
        AutoResetEvent StopEvent = new AutoResetEvent(false);

        public static IGlContext Context;

        public static bool AlreadyInitialized;

        public bool IsCurrentWindow;

        public GpuStateStruct* GeContext;

        //public static object GpuLock = new object();

        private Matrix4x4 _worldViewProjectionMatrix = Matrix4x4.Identity;
        private Matrix4x4 _textureMatrix = Matrix4x4.Identity;

        private GLShader _shader;
        private GLBuffer _lightBuf;

        bool _doPrimStart;
        int _Scale = 0;
        VertexTypeStruct _cachedVertexType;
        GuPrimitiveType _primitiveType;

        //public GLRenderTarget FrameTarget;
        public GLFrameBuffer FrameFB;
        public GLTexture2D FrameTex2D, FrameTexDEPTH;
        public GLTexture2D LogicTex2D;
        public TextureCacheGL TextureCache;
        public TextureGL CurrentTextureCache;
        public GLTextureUnit TexUnit;
        public int _Width, _Height;

        public class ShaderInfoClass
        {
            public GlUniform matrixWorldViewProjection;
            public GlUniform matrixTexture;
            public GlUniform matrixBones;

            public GlUniform hasPerVertexColor;
            public GlUniform hasTexture;
            public GlUniform hasReversedNormal;
            public GlUniform clearingMode;

            public GlUniform texture0;
            public GlUniform uniformColor;
            public GlUniform TextureMode;

            public GlUniform colorTest;

            public GlUniform alphaTest;
            public GlUniform alphaFunction;
            public GlUniform alphaValue;
            public GlUniform alphaMask;

            public GlUniform weightCount;

            public GlUniform tfx;
            public GlUniform tcc;

            public GlAttribute vertexPosition;
            public GlAttribute vertexTexCoords;
            public GlAttribute vertexColor;
            public GlAttribute vertexNormal;

            public GlAttribute vertexWeight0;
            public GlAttribute vertexWeight1;
            public GlAttribute vertexWeight2;
            public GlAttribute vertexWeight3;
            public GlAttribute vertexWeight4;
            public GlAttribute vertexWeight5;
            public GlAttribute vertexWeight6;
            public GlAttribute vertexWeight7;

            public GlUniform lightenable;
            public GlUniform matrixWorld;
            public GlUniform matrixView;

            public GlUniform lopEnabled;
            public GlUniform backtex;
            public GlUniform lop;
        }

        ShaderInfoClass ShaderInfo = new ShaderInfoClass();

        //GE RenderTarget
        //Bind to gpuState->TextureMappingState.TextureState.Mipmap0.Address
        //Bind to gpuState->DrawBufferState.Address
        public readonly Dictionary<uint, GLTexture2D> GeTexture = new Dictionary<uint, GLTexture2D>();

        public GLBackEnd()
        {
            TextureCache = new TextureCacheGL(Memory, this);
            VertexReader = new VertexReader();
        }

        public static string GlGetString(int name) => GL.GetStringStr(name);

        private void UpdateScale(int scaleViewport)
        {
            if (_Scale == scaleViewport) return;
            _Scale = scaleViewport;

            _Width = 480 * scaleViewport;
            _Height = 272 * scaleViewport;

            FrameFB?.Dispose();
            FrameTex2D?.Dispose();
            FrameTexDEPTH?.Dispose();

            FrameFB = GLFrameBuffer.Create();
            FrameTexDEPTH = GLTexture2D.Create().SetFormat(TextureFormat.DEPTH).SetSize(_Width, _Height);
            FrameFB.AttachTexture(FramebufferAttachment.DepthAttachment, FrameTexDEPTH);
            FrameFB.Bind();

            LogicTex2D?.Dispose();
            LogicTex2D = GLTexture2D.Create().SetSize(_Width, _Height);
        }

        public override void InitSynchronizedOnce(IntPtr TargetHwnd)
        {
            ScaleViewport = PspStoredConfig.RenderScale;

            if (!AlreadyInitialized)
            {
                if (TargetHwnd == IntPtr.Zero)
                {
                    Console.Out.WriteLineColored(ConsoleColor.White, $"   -> OpenGL Windowless Mode");
                    Context = GlContextFactory.CreateWindowless();
                }
                else
                {
                    Console.Out.WriteLineColored(ConsoleColor.White, $"   -> OpenGL Window HWND: {TargetHwnd}");
                    Context = GlContextFactory.CreateFromWindowHandle(TargetHwnd);
                }

                Context.MakeCurrent();


                Console.Out.WriteLineColored(ConsoleColor.White, "   -> OpenGL Context Version: {0}", GlGetString(GL.GL_VERSION));
                Console.Out.WriteLineColored(ConsoleColor.White, "   -> Depth Bits: {0}", GL.GetInteger(GL.GL_DEPTH_BITS));
                Console.Out.WriteLineColored(ConsoleColor.White, "   -> Stencil Bits: {0}", GL.GetInteger(GL.GL_STENCIL_BITS));
                Console.Out.WriteLineColored(ConsoleColor.White, "   -> Color Bits: {0},{1},{2},{3}",
                    GL.GetInteger(GL.GL_RED_BITS), GL.GetInteger(GL.GL_GREEN_BITS),
                    GL.GetInteger(GL.GL_BLUE_BITS), GL.GetInteger(GL.GL_ALPHA_BITS));

                if (GL.GetInteger(GL.GL_STENCIL_BITS) <= 0)
                {
                    Console.Error.WriteLineColored(ConsoleColor.Red, "   -> No stencil bits available!");
                    //throw new Exception("Couldn't initialize opengl");
                }

                UpdateScale(PspStoredConfig.RenderScale);

                _verticesPositionBuffer = GLBuffer.Create();
                _verticesNormalBuffer = GLBuffer.Create();
                _verticesTexcoordsBuffer = GLBuffer.Create();
                _verticesColorsBuffer = GLBuffer.Create();
                _verticesWeightsBuffer = GLBuffer.Create();

                _shader = new GLShader(Shaders.ShaderVert, Shaders.ShaderFrag);

                //Console.WriteLine("###################################");
                //foreach (var uniform in _shader.Uniforms) Console.WriteLine(uniform);
                //foreach (var attribute in _shader.Attributes) Console.WriteLine(attribute);
                //Console.WriteLine("###################################");

                _shader.BindUniformsAndAttributes(ShaderInfo);

                _shader.BindUniformBlock("LightBlock", 0);

                _lightBuf = GLBuffer.Create(BufferTarget.UniformBuffer, BufferUsage.DynamicDraw);

                TexUnit = GLTextureUnit.CreateAtIndex(0);

                Context.ReleaseCurrent();

                AlreadyInitialized = true;
            }
        }

        public override void StopSynchronized()
        {
            //Running = false;
            //StopEvent.WaitOne();

            if (AlreadyInitialized)
            {
                _verticesPositionBuffer.Dispose();
                _verticesNormalBuffer.Dispose();
                _verticesTexcoordsBuffer.Dispose();
                _verticesColorsBuffer.Dispose();
                _verticesWeightsBuffer.Dispose();

                _shader.Dispose();
                _lightBuf.Dispose();

                FrameFB?.Dispose();
                FrameTexDEPTH?.Dispose();

                LogicTex2D?.Dispose();

                foreach (var item in GeTexture)
                {
                    item.Value?.Dispose();
                }
                GeTexture.Clear();

                CurrentTextureCache?.Dispose();

                Context.Dispose();
            }
        }

        public override void SetCurrent()
        {
            if (!IsCurrentWindow)
            {
                Context.MakeCurrent();
                IsCurrentWindow = true;
            }
        }

        public override void UnsetCurrent()
        {
            Context.ReleaseCurrent();
            IsCurrentWindow = false;
        }

        public override void DrawCurvedSurface(GlobalGpuState GlobalGpuState, GpuStateStruct* GpuStateStruct, VertexInfo[,] Patch, int UCount, int VCount)
        {
            if (Patch == null) return;
            if (Patch.Length == 0) return;

            int s_len = Patch.GetLength(0);
            int t_len = Patch.GetLength(1);

            if (s_len <= 1 || t_len <= 1) return;

            GeContext = GpuStateStruct;
            VertexType = GeContext->VertexState.Type;

            PrepareStateCommon(GeContext, ScaleViewport);

            if (GeContext->ClearingMode)
            {
                PrepareStateClear(GeContext);
            }
            else
            {
                PrepareStateDraw(GeContext);
            }

            PrepareStateMatrix(GeContext, out _worldViewProjectionMatrix);

            PrepareShaderTexSet();

            float s_len_float = s_len;
            float t_len_float = t_len;

            var mipmap0 = GeContext->TextureMappingState.TextureState.Mipmap0;
            float mipmapWidth = mipmap0.TextureWidth != 0 ? mipmap0.TextureWidth : 1.0f;
            float mipmapHeight = mipmap0.TextureHeight != 0 ? mipmap0.TextureHeight : 1.0f;

            ResetVertex();

            for (int t = 0; t < t_len - 1; t++)
            {
                for (int s = 0; s < s_len - 1; s++)
                {
                    var v1 = Patch[s + 0, t + 0];
                    var v2 = Patch[s + 0, t + 1];
                    var v3 = Patch[s + 1, t + 1];
                    var v4 = Patch[s + 1, t + 0];

                    if (VertexType.HasTexture)
                    {
                        v1.Texture.X = ((float)s + 0) * mipmapWidth / s_len_float;
                        v1.Texture.Y = ((float)t + 0) * mipmapHeight / t_len_float;

                        v2.Texture.X = ((float)s + 0) * mipmapWidth / s_len_float;
                        v2.Texture.Y = ((float)t + 1) * mipmapHeight / t_len_float;

                        v3.Texture.X = ((float)s + 1) * mipmapWidth / s_len_float;
                        v3.Texture.Y = ((float)t + 1) * mipmapHeight / t_len_float;

                        v4.Texture.X = ((float)s + 1) * mipmapWidth / s_len_float;
                        v4.Texture.Y = ((float)t + 0) * mipmapHeight / t_len_float;
                    }

                    PutVertex(v1);
                    PutVertex(v2);
                    PutVertex(v3);

                    PutVertex(v1);
                    PutVertex(v3);
                    PutVertex(v4);
                }
            }

            DrawVertices(GLGeometry.GL_TRIANGLES);
            ResetVertex();
        }

        public override void DrawSpline(GlobalGpuState GlobalGpuState, GpuStateStruct* GpuStateStruct, VertexInfo[,] Patch,
            int sp_ucount, int sp_vcount, int sp_utype, int sp_vtype, int normalizedUType, int normalizedVType)
        {
            if (Patch == null || Patch.Length == 0) return;

            int patchUCount = Patch.GetLength(0);
            int patchVCount = Patch.GetLength(1);

            if (patchUCount <= 1 || patchVCount <= 1) return;

            GeContext = GpuStateStruct;
            VertexType = GeContext->VertexState.Type;

            PrepareStateCommon(GeContext, ScaleViewport);

            if (GeContext->ClearingMode)
            {
                PrepareStateClear(GeContext);
            }
            else
            {
                PrepareStateDraw(GeContext);
            }

            PrepareStateMatrix(GeContext, out _worldViewProjectionMatrix);

            PrepareShaderTexSet();

            var mipmap0 = GeContext->TextureMappingState.TextureState.Mipmap0;
            float mipmapWidth = mipmap0.TextureWidth > 0 ? mipmap0.TextureWidth : 1.0f;
            float mipmapHeight = mipmap0.TextureHeight > 0 ? mipmap0.TextureHeight : 1.0f;

            ResetVertex();

            int uSegments = patchUCount - 1;  // Patch的U方向分段数 = 顶点数-1
            int vSegments = patchVCount - 1;  // Patch的V方向分段数 = 顶点数-1

            for (int v = 0; v < vSegments; v++)
            {
                for (int u = 0; u < uSegments; u++)
                {
                    var v1 = Patch[u, v];
                    var v2 = Patch[u, v + 1];
                    var v3 = Patch[u + 1, v + 1];
                    var v4 = Patch[u + 1, v];

                    if (VertexType.HasTexture)
                    {
                        float uvU = (float)u / uSegments;
                        float uvV = (float)v / vSegments;
                        float uvU1 = (float)(u + 1) / uSegments;
                        float uvV1 = (float)(v + 1) / vSegments;

                        v1.Texture.X = uvU * mipmapWidth;
                        v1.Texture.Y = uvV * mipmapHeight;

                        v2.Texture.X = uvU * mipmapWidth;
                        v2.Texture.Y = uvV1 * mipmapHeight;

                        v3.Texture.X = uvU1 * mipmapWidth;
                        v3.Texture.Y = uvV1 * mipmapHeight;

                        v4.Texture.X = uvU1 * mipmapWidth;
                        v4.Texture.Y = uvV * mipmapHeight;
                    }

                    // 第一个三角面：v1 → v2 → v3（顺时针）
                    PutVertex(v1);
                    PutVertex(v2);
                    PutVertex(v3);

                    // 第二个三角面：v1 → v3 → v4（顺时针）
                    PutVertex(v1);
                    PutVertex(v3);
                    PutVertex(v4);
                }
            }

            DrawVertices(GLGeometry.GL_TRIANGLES);
            ResetVertex();
        }

        public override void DrawVideo(uint frameBufferAddress, OutputPixel* outputPixel, int width, int height)
        {

        }

        public override void DrawBBOX(GlobalGpuState GlobalGpuState, GpuStateStruct* GpuStateStruct, int VertexCount)
        {
            //BeforeDraw(GpuStateStruct);

            //var type = GpuStateStruct.VertexState.Type;
            //var state = GpuStateStruct.VertexState;
            //var SkinningState = GpuStateStruct.SkinningState;

            //for (int i = 0; i < VertexCount; i++)
            //{

            //    if (type.Weight != 0 && type.Position != 0)
            //    {
            //        //doSkinning(SkinningState, type, state);
            //    }

            //    int vertexIndex = i % 8;

            //    Vertices[vertexIndex][0] = v.p[0];
            //    Vertices[vertexIndex][1] = v.p[1];
            //    Vertices[vertexIndex][2] = v.p[2];

            //    if (vertexIndex == 7)
            //    {
            //        //PutVertex
            //    }
            //}

            //DrawVertices(GLGeometry.GL_TRIANGLES);
            //ResetVertex();
        }

        public override void InvalidateCache(uint address, int size)
        {
            //ConsoleUtils.SaveRestoreConsoleColor(ConsoleColor.White, () =>
            //{
            //	//foreach ()
            //	//Console.WriteLine("OnMemoryWrite: {0:X8}, {1}", Address, Size);
            //	//foreach (var DrawBufferTexture in DrawBufferTextures)
            //	//{
            //	//	Console.WriteLine("::{0:X8}", DrawBufferTexture.Key.Address);
            //	//}
            //});
        }

        public override void StartCapture()
        {
        }

        public override void EndCapture()
        {
        }

        public override void PrimStart(GlobalGpuState globalGpuState, GpuStateStruct* gpuState, GuPrimitiveType primitiveType)
        {
            GeContext = gpuState;
            _primitiveType = primitiveType;
            _doPrimStart = true;
            ResetVertex();

            if (ShaderInfo != null)
            {
                ShaderInfo.lopEnabled.Set(gpuState->LogicalOperationState.Enabled);

                if (gpuState->LogicalOperationState.Enabled)
                {
                    FrameFB.Bind(FramebufferTarget.ReadFramebuffer);

                    LogicTex2D.Bind();

                    GL.CopyTexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_RGBA, 0, 0, FrameFB.TextureColor.Width, FrameFB.TextureColor.Height, 0);

                    FrameFB.Bind(FramebufferTarget.Framebuffer);

                    FrameFB.TextureColor.Bind();

                    ShaderInfo.backtex.Set(
                        GLTextureUnit.CreateAtIndex(1)
                        .SetFiltering(GLScaleFilter.Linear)
                        .SetWrap(GLWrap.ClampToEdge)
                        .SetTexture(LogicTex2D));

                    ShaderInfo.lop.Set((int)gpuState->LogicalOperationState.Operation);
                }
            }
        }

        public override void Prim(ushort vertexCount, bool IsPatchPrim = false)
        {
            VertexType = GeContext->VertexState.Type;

            if (_doPrimStart || VertexType != _cachedVertexType)
            {
                _cachedVertexType = VertexType;
                _doPrimStart = false;

                PrepareStateCommon(GeContext, ScaleViewport);

                if (GeContext->ClearingMode)
                {
                    PrepareStateClear(GeContext);
                }
                else
                {
                    PrepareStateDraw(GeContext);
                }

                PrepareStateMatrix(GeContext, out _worldViewProjectionMatrix);

                PrepareShaderTexSet();
            }

            uint morpingVertexCount, totalVerticesWithoutMorphing;

            PrepareVertexs(GeContext, out totalVerticesWithoutMorphing, vertexCount, out morpingVertexCount);

            var z = 0;
            var vertexInfoFloatCount = sizeof(VertexInfo) / sizeof(float);

            fixed (VertexInfo* verticesPtr = Vertices)
            {
                if (morpingVertexCount == 1)
                {
                    VertexReader.ReadVertices(0, verticesPtr, (int)totalVerticesWithoutMorphing);
                }
                else
                {
                    VertexInfo tempVertexInfo;
                    var componentsIn = (float*)&tempVertexInfo;
                    for (var n = 0; n < totalVerticesWithoutMorphing; n++)
                    {
                        var componentsOut = (float*)&verticesPtr[n];
                        for (var cc = 0; cc < vertexInfoFloatCount; cc++) componentsOut[cc] = 0;
                        for (var m = 0; m < morpingVertexCount; m++)
                        {
                            VertexReader.ReadVertex(z++, &tempVertexInfo);
                            for (var cc = 0; cc < vertexInfoFloatCount; cc++)
                                componentsOut[cc] += componentsIn[cc] * GeContext->MorphingState.MorphWeight(m);
                        }
                        verticesPtr[n].Normal = verticesPtr[n].Normal.Normalize();
                    }
                }
            }

            if (IsPatchPrim)
            {
                PPrim(vertexCount);
            }
            else if (_primitiveType == GuPrimitiveType.Sprites)
            {
                HandleSprites(vertexCount);
            }
            else
            {
                HandleStandardVertices(vertexCount);
            }
        }

        private void PPrim(ushort vertexCount)
        {
            GL.Disable(GL.GL_CULL_FACE);

            var mipmap0 = GeContext->TextureMappingState.TextureState.Mipmap0;
            float mipmapWidth = mipmap0.TextureWidth > 0 ? mipmap0.TextureWidth : 1.0f;
            float mipmapHeight = mipmap0.TextureHeight > 0 ? mipmap0.TextureHeight : 1.0f;

            VertexInfo v;
            for (var n = 0; n < vertexCount; n++)
            {
                readVertex(n, out v);

                // Patch 需要归一化?
                if (VertexType.HasTexture)
                {
                    v.Texture.X = v.Texture.X / mipmapWidth;
                    v.Texture.Y = v.Texture.Y / mipmapHeight;
                }

                PutVertex(v);
            }
        }

        private void HandleSprites(ushort vertexCount)
        {
            GL.Disable(GL.GL_CULL_FACE);
            for (var n = 0; n < vertexCount; n += 2)
            {
                VertexInfo v0, v1, v2, v3;

                readVertex(n + 0, out v0);
                readVertex(n + 1, out v3);

                VertexUtils.GenerateTriangleStripFromSpriteVertices(ref v0, out v1, out v2, ref v3);

                if (n > 0)
                {
                    // 连接上一批次的顶点
                    PutVertexIndexRelative(-1);
                    PutVertexIndexRelative(0);
                }

                PutVertices(v0, v1, v2, v3);
            }
        }
        private void HandleStandardVertices(ushort vertexCount)
        {
            VertexInfo vertexInfo;
            for (var n = 0; n < vertexCount; n++)
            {
                readVertex(n, out vertexInfo);
                PutVertex(vertexInfo);
            }
        }

        public override void PrimEnd()
        {
            EndVertex();
        }

        public override void BeforeDraw(GpuStateStruct* gpuState)
        {
            var Address = gpuState->DrawBufferState.Address;

            GLTexture2D Texture;

            if (!GeTexture.TryGetValue(Address, out Texture))
            {
                Texture = GLTexture2D.Create().SetSize(_Width, _Height);

                GeTexture.Add(Address, Texture);
            }

            FrameFB.AttachTexture(FramebufferAttachment.ColorAttachment0, Texture);

            FrameFB.Bind(FramebufferTarget.Framebuffer);
        }

        public override void Finish(GpuStateStruct* gpuState)
        {
        }

        public override void End(GpuStateStruct* gpuState)
        {
        }

        public override void Sync(GpuStateStruct* gpuState)
        {
        }

        public override void TextureFlush(GpuStateStruct* gpuState)
        {
            TextureCache.RecheckAll();
        }

        public override void TextureSync(GpuStateStruct* gpuState)
        {
        }

        public override void Transfer(GpuStateStruct* gpuState)
        {
            var textureTransferState = gpuState->TextureTransferState;

            // 如果写入目标是当前 DrawBuffer（地址和行宽与 DrawBuffer 匹配），走专用路径以获得更高效的拷贝
            if (
                textureTransferState.DestinationAddress.Address == gpuState->DrawBufferState.Address &&
                textureTransferState.DestinationLineWidth == gpuState->DrawBufferState.Width &&
                textureTransferState.BytesPerPixel == gpuState->DrawBufferState.BytesPerPixel
            )
            {
                TransferToFrameBuffer(gpuState);
            }
            else
            {
                TransferGeneric(gpuState);
            }
        }

        private void TransferToFrameBuffer(GpuStateStruct* gpuState)
        {
            var textureTransferState = gpuState->TextureTransferState;

            var sourceX = textureTransferState.SourceX;
            var sourceY = textureTransferState.SourceY;
            var destinationX = textureTransferState.DestinationX;
            var destinationY = textureTransferState.DestinationY;
            var bytesPerPixel = textureTransferState.BytesPerPixel;

            var drawBuffer = gpuState->DrawBufferState;

            if (bytesPerPixel != drawBuffer.BytesPerPixel || textureTransferState.DestinationLineWidth != drawBuffer.Width)
            {
                TransferGeneric(gpuState);
                return;
            }

            var sourceLineWidth = textureTransferState.SourceLineWidth;
            var destLineWidth = textureTransferState.DestinationLineWidth; // 应当等于 drawBuffer.Width

            long sourceTotalBytes = (long)sourceLineWidth * textureTransferState.Height * bytesPerPixel;
            long destTotalBytes = (long)destLineWidth * textureTransferState.Height * bytesPerPixel;

            var sourcePtr = (byte*)Memory.PspAddressToPointerSafe(textureTransferState.SourceAddress.Address, (int)Math.Max(0L, sourceTotalBytes));
            var destPtr = (byte*)Memory.PspAddressToPointerSafe(drawBuffer.Address, (int)Math.Max(0L, destTotalBytes));

            if (sourcePtr == null || destPtr == null)
            {
                Console.Error.WriteLine("TransferToFrameBuffer: Invalid memory pointer(s).");
                return;
            }

            for (uint y = 0; y < textureTransferState.Height; y++)
            {
                var rowSourceOffset = (uint)(sourceLineWidth * (y + sourceY) + sourceX);
                var rowDestinationOffset = (uint)(destLineWidth * (y + destinationY) + destinationX);

                PointerUtils.Memcpy(
                    destPtr + rowDestinationOffset * bytesPerPixel,
                    sourcePtr + rowSourceOffset * bytesPerPixel,
                    textureTransferState.Width * bytesPerPixel
                );
            }
            // 不在此强制刷新 GPU 纹理缓存；上层或 RenderbufferManager 会在需要时使用 DrawBuffer 数据。
        }

        private void TransferGeneric(GpuStateStruct* gpuState)
        {
            var textureTransferState = gpuState->TextureTransferState;

            var sourceX = textureTransferState.SourceX;
            var sourceY = textureTransferState.SourceY;
            var destinationX = textureTransferState.DestinationX;
            var destinationY = textureTransferState.DestinationY;
            var bytesPerPixel = textureTransferState.BytesPerPixel;

            var sourceTotalBytes = (long)textureTransferState.SourceLineWidth * textureTransferState.Height * bytesPerPixel;
            var destinationTotalBytes = (long)textureTransferState.DestinationLineWidth * textureTransferState.Height * bytesPerPixel;

            var sourcePointer = (byte*)Memory.PspAddressToPointerSafe(textureTransferState.SourceAddress.Address, (int)Math.Max(0L, sourceTotalBytes));
            var destinationPointer = (byte*)Memory.PspAddressToPointerSafe(textureTransferState.DestinationAddress.Address, (int)Math.Max(0L, destinationTotalBytes));

            if (sourcePointer == null || destinationPointer == null)
            {
                Console.Error.WriteLine("TransferGeneric: Invalid memory pointer(s).");
                return;
            }

            for (uint y = 0; y < textureTransferState.Height; y++)
            {
                var rowSourceOffset = (uint)(
                    textureTransferState.SourceLineWidth * (y + sourceY) + sourceX
                );
                var rowDestinationOffset = (uint)(
                    textureTransferState.DestinationLineWidth * (y + destinationY) + destinationX
                );
                PointerUtils.Memcpy(
                    destinationPointer + rowDestinationOffset * bytesPerPixel,
                    sourcePointer + rowSourceOffset * bytesPerPixel,
                    textureTransferState.Width * bytesPerPixel
                );
            }
        }

    }
}