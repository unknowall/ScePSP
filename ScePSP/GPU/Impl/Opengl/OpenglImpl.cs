#define ENABLE_TEXTURES

using ScePSP.Core.Gpu.Formats;
using ScePSP.Core.Gpu.State;
using ScePSP.Core.Gpu.VertexReading;
using ScePSP.Core.Types;
using ScePSP.Utils;
using ScePSPPlatform.GL;
using ScePSPPlatform.GL.Utils;
using ScePSPUtils;
using ScePSPUtils.Drawing;
using System;
using System.Globalization;
using System.Numerics;
using System.Threading;

namespace ScePSP.Core.Gpu.Impl.Opengl
{
    public unsafe class OpenglGpuImpl : GpuImpl, IInjectInitialize
    {
        public override bool IsWorking => true;

        public TextureCacheOpengl TextureCache;

        private new GpuStateStruct GpuState;

        AutoResetEvent StopEvent = new AutoResetEvent(false);

        bool Running = true;

        public static IGlContext OpenglContext;

        public static bool AlreadyInitialized;

        public bool IsCurrentWindow;

        private object PspWavefrontObjWriterLock = new object();

        private PspWavefrontObjWriter _pspWavefrontObjWriter = null;

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

        //public static object GpuLock = new object();

        public class FastList<T>
        {
            public int Length = 0;

            public T[] Buffer = new T[1024];

            public void Reset() => Length = 0;

            public void Add(T item)
            {
                if (Length >= Buffer.Length) Buffer = Buffer.ResizedCopy(Buffer.Length * 2);
                Buffer[Length++] = item;
            }
        }

        private readonly FastList<Vector3> _verticesPosition = new FastList<Vector3>();
        private readonly FastList<Vector3> _verticesNormal = new FastList<Vector3>();
        private readonly FastList<Vector3> _verticesTexcoords = new FastList<Vector3>();
        private readonly FastList<RgbaFloat> _verticesColors = new FastList<RgbaFloat>();
        private readonly FastList<VertexInfoWeights> _verticesWeights = new FastList<VertexInfoWeights>();

        private GLBuffer _verticesPositionBuffer;
        private GLBuffer _verticesNormalBuffer;
        private GLBuffer _verticesTexcoordsBuffer;
        private GLBuffer _verticesColorsBuffer;
        private GLBuffer _verticesWeightsBuffer;

        private FastList<uint> _indicesList = new FastList<uint>();

        private Matrix4x4 _worldViewProjectionMatrix = Matrix4x4.Identity;
        private Matrix4x4 _textureMatrix = Matrix4x4.Identity;

        public RenderbufferManager RenderbufferManager { get; private set; }
        private GLShader _shader;

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
        }

        ShaderInfoClass ShaderInfo = new ShaderInfoClass();

        [Inject] InjectContext InjectContext;

        void IInjectInitialize.Initialize()
        {
            RenderbufferManager = new RenderbufferManager(this);
            TextureCache = new TextureCacheOpengl(Memory, this, InjectContext);
            VertexReader = new VertexReader();
        }

        private void DrawInitVertices()
        {
            //Console.WriteLine(WGL.wglGetCurrentContext());
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
        }

        private void PrepareDrawStateFirst()
        {
            if (_shader == null) DrawInitVertices();

            var vertexType = GpuState.VertexState.Type;

            ShaderInfo.matrixWorldViewProjection.Set(_worldViewProjectionMatrix);
            ShaderInfo.matrixTexture.Set(_textureMatrix);

            try
            {
                ShaderInfo.uniformColor.NoWarning().Set(GpuState.LightingState.AmbientModelColor.ToVector4());
            }
            catch
            {
            }

            ShaderInfo.hasPerVertexColor.Set(vertexType.HasColor);
            ShaderInfo.clearingMode.Set(GpuState.ClearingMode);
            ShaderInfo.hasTexture.Set(GpuState.TextureMappingState.Enabled);
            ShaderInfo.weightCount.Set(vertexType.RealSkinningWeightCount);

            if (vertexType.HasWeight && ShaderInfo.matrixBones != null && ShaderInfo.matrixBones.IsAvailable)
            {
                int uniformArrayLength = Math.Max(0, ShaderInfo.matrixBones.ArrayLength);

                // PSP 最多8根骨骼
                if (uniformArrayLength > 0)
                {
                    var bones = new Matrix4x4[uniformArrayLength];
                    for (int i = 0; i < uniformArrayLength; i++)
                    {
                        switch (i)
                        {
                            case 0: bones[i] = GpuState.SkinningState.BoneMatrix0; break;
                            case 1: bones[i] = GpuState.SkinningState.BoneMatrix1; break;
                            case 2: bones[i] = GpuState.SkinningState.BoneMatrix2; break;
                            case 3: bones[i] = GpuState.SkinningState.BoneMatrix3; break;
                            case 4: bones[i] = GpuState.SkinningState.BoneMatrix4; break;
                            case 5: bones[i] = GpuState.SkinningState.BoneMatrix5; break;
                            case 6: bones[i] = GpuState.SkinningState.BoneMatrix6; break;
                            case 7: bones[i] = GpuState.SkinningState.BoneMatrix7; break;
                            default: bones[i] = Matrix4x4.Identity; break;
                        }
                    }

                    try
                    {
                        ShaderInfo.matrixBones.Set(bones);
                    }
                    catch (Exception)
                    {
                        //shader 可能没有该 uniform 或长度不匹配
                    }
                }
            }

            if (vertexType.HasTexture && GpuState.TextureMappingState.Enabled)
            {
                var textureState = GpuState.TextureMappingState.TextureState;

                ShaderInfo.tfx.Set((int)textureState.Effect);
                ShaderInfo.tcc.Set((int)textureState.ColorComponent);
                ShaderInfo.colorTest.NoWarning().Set(GpuState.ColorTestState.Enabled);

                ShaderInfo.alphaTest.Set(GpuState.AlphaTestState.Enabled);
                ShaderInfo.alphaFunction.Set((int)GpuState.AlphaTestState.Function);
                ShaderInfo.alphaMask.NoWarning().Set(GpuState.AlphaTestState.Mask);
                ShaderInfo.alphaValue.Set(GpuState.AlphaTestState.Value);

                ShaderInfo.texture0.Set(GLTextureUnit.CreateAtIndex(0)
                    .SetWrap(
                        (GLWrap)(textureState.WrapU == WrapMode.Repeat ? GL.GL_REPEAT : GL.GL_CLAMP_TO_EDGE),
                        (GLWrap)(textureState.WrapV == WrapMode.Repeat ? GL.GL_REPEAT : GL.GL_CLAMP_TO_EDGE)
                    )
                    .SetFiltering(
                        (GLScaleFilter)(textureState.FilterMinification == TextureFilter.Linear
                            ? GL.GL_LINEAR
                            : GL.GL_NEAREST),
                        (GLScaleFilter)(textureState.FilterMagnification == TextureFilter.Linear
                            ? GL.GL_LINEAR
                            : GL.GL_NEAREST)
                    )
                    .SetTexture(RenderbufferManager.TextureCacheGetAndBind(GpuState))
                );
            }
        }

        private void DrawVertices(GLGeometry type)
        {
            //Console.Out.WriteLineColored(ConsoleColor.Green, $"GE Prim Vertices: {_indicesList.Length} GLGeometr: {type.ToString()}");
            //int i = 0;
            //foreach (var v in _verticesPosition.Buffer)
            //{
            //    i++;
            //    Console.Out.WriteLineColored(ConsoleColor.Green, $"    Pos: {v.X}, {v.Y}, {v.Z}");
            //    if (i >= _indicesList.Length) break;
            //}

            ShaderInfo.hasReversedNormal.NoWarning().Set(VertexType.ReversedNormal);

            _shader.Draw(type, _indicesList.Buffer, _indicesList.Length, () =>
            {
                // 位置
                if (VertexType.HasPosition)
                {
                    _verticesPositionBuffer.SetData(_verticesPosition.Buffer, 0, _verticesPosition.Length);
                    ShaderInfo.vertexPosition.SetData<float>(_verticesPositionBuffer, 3, 0, sizeof(Vector3), false);
                }
                else
                {
                    ShaderInfo.vertexPosition.UnsetData();
                }
                // 纹理
                if (VertexType.HasTexture)
                {
                    _verticesTexcoordsBuffer.SetData(_verticesTexcoords.Buffer, 0, _verticesTexcoords.Length);
                    ShaderInfo.vertexTexCoords.SetData<float>(_verticesTexcoordsBuffer, 3, 0, sizeof(Vector3), false);
                }
                else
                {
                    ShaderInfo.vertexTexCoords.UnsetData();
                }
                // 顶点颜色
                if (VertexType.HasColor)
                {
                    _verticesColorsBuffer.SetData(_verticesColors.Buffer, 0, _verticesColors.Length);
                    ShaderInfo.vertexColor.SetData<float>(_verticesColorsBuffer, 4, 0, sizeof(RgbaFloat), false);
                }
                else
                {
                    ShaderInfo.vertexColor.UnsetData();
                }
                // 法线
                if (VertexType.HasNormal)
                {
                    _verticesNormalBuffer.SetData(_verticesNormal.Buffer, 0, _verticesNormal.Length);
                    ShaderInfo.vertexNormal.NoWarning().SetData<float>(_verticesNormalBuffer, 3, 0, sizeof(Vector3), false);
                }
                else
                {
                    ShaderInfo.vertexNormal.NoWarning().UnsetData();
                }
                // 骨骼权重：如果当前顶点类型包含权重，则上传需要的权重属性并取消未用属性绑定
                var realWeightCount = VertexType.RealSkinningWeightCount;
                if (VertexType.HasWeight && realWeightCount > 0)
                {
                    _verticesWeightsBuffer.SetData(_verticesWeights.Buffer, 0, _verticesWeights.Length);
                    var vertexWeights = new[]
                    {
                        ShaderInfo.vertexWeight0, ShaderInfo.vertexWeight1, ShaderInfo.vertexWeight2,
                        ShaderInfo.vertexWeight3, ShaderInfo.vertexWeight4, ShaderInfo.vertexWeight5,
                        ShaderInfo.vertexWeight6, ShaderInfo.vertexWeight7
                    };
                    for (var n = 0; n < 8; n++)
                    {
                        if (n < realWeightCount)
                        {
                            // elementSize = 1 (单个 float), offset = n * sizeof(float), stride = sizeof(VertexInfoWeights)
                            vertexWeights[n].SetData<float>(_verticesWeightsBuffer, 1, n * sizeof(float), sizeof(VertexInfoWeights), false);
                        }
                        else
                        {
                            vertexWeights[n].UnsetData();
                        }
                    }
                }
                else
                {
                    ShaderInfo.vertexWeight0.UnsetData();
                    ShaderInfo.vertexWeight1.UnsetData();
                    ShaderInfo.vertexWeight2.UnsetData();
                    ShaderInfo.vertexWeight3.UnsetData();
                    ShaderInfo.vertexWeight4.UnsetData();
                    ShaderInfo.vertexWeight5.UnsetData();
                    ShaderInfo.vertexWeight6.UnsetData();
                    ShaderInfo.vertexWeight7.UnsetData();
                }
            });
        }

        private void ResetVertex()
        {
            _verticesPosition.Reset();
            _verticesNormal.Reset();
            _verticesWeights.Reset();
            _verticesTexcoords.Reset();
            _verticesColors.Reset();
            _indicesList.Reset();
        }

        private void PutVertices(params VertexInfo[] vertexInfoList)
        {
            foreach (var vertexInfo in vertexInfoList) PutVertex(vertexInfo);
        }

        private void PutVertexIndexRelative(int offset)
        {
            PutVertexIndex(_verticesPosition.Length + offset);
        }

        private void PutVertexIndex(int vertexIndex)
        {
            _indicesList.Add((uint)vertexIndex);
        }

        private void PutVertex(VertexInfo vertexInfo)
        {
            //Console.Out.WriteLineColored(ConsoleColor.Yellow, $"PutVertex {vertexInfo.ToString()}");

            _CapturePutVertex(ref vertexInfo);

            PutVertexIndex(_verticesPosition.Length);

            _verticesPosition.Add(vertexInfo.Position.ToRVector3());
            _verticesNormal.Add(vertexInfo.Normal.ToRVector3());
            _verticesTexcoords.Add(vertexInfo.Texture.ToRVector3());
            _verticesColors.Add(new RgbaFloat(vertexInfo.Color));

            var weightsStruct = new VertexInfoWeights(vertexInfo);

            var count = VertexType.RealSkinningWeightCount;
            if (count <= 0) count = 0; // defensive
            float sum = 0f;
            // sum 仅计算前 count 个权重
            for (int i = 0; i < count && i < 8; i++)
            {
                sum += weightsStruct.W[i];
            }
            if (count > 0)
            {
                if (sum <= float.Epsilon)
                {
                    // 如果总权重为 0，则设置第一个权重为 1，避免除零并保持顶点不变形
                    weightsStruct.W[0] = 1.0f;
                    for (int i = 1; i < count && i < 8; i++) weightsStruct.W[i] = 0.0f;
                }
                else
                {
                    // 归一化前 count 个权重
                    for (int i = 0; i < count && i < 8; i++) weightsStruct.W[i] = weightsStruct.W[i] / sum;
                }
                // 其余槽置 0
                for (int i = count; i < 8; i++) weightsStruct.W[i] = 0.0f;
            }
            else
            {
                // 没有权重
                for (int i = 0; i < 8; i++) weightsStruct.W[i] = 0.0f;
            }

            _verticesWeights.Add(weightsStruct);
        }

        public override void StartCapture()
        {
            lock (PspWavefrontObjWriterLock)
            {
                _pspWavefrontObjWriter = new PspWavefrontObjWriter(new WavefrontObjWriter(ApplicationPaths.AssertPath + "/gpu_frame.obj"));
            }
        }

        public override void EndCapture()
        {
            lock (PspWavefrontObjWriterLock)
            {
                _pspWavefrontObjWriter.End();
                _pspWavefrontObjWriter = null;
            }
        }

        private void _CapturePrimitive(GuPrimitiveType primitiveType, uint vertexAddress, int vetexCount, ref VertexTypeStruct vertexType, Action action)
        {
            if (_pspWavefrontObjWriter != null)
            {
                lock (PspWavefrontObjWriterLock)
                    _pspWavefrontObjWriter.StartPrimitive(GpuState, primitiveType, vertexAddress, vetexCount, ref vertexType);
                try
                {
                    action();
                }
                finally
                {
                    lock (PspWavefrontObjWriterLock) _pspWavefrontObjWriter.EndPrimitive();
                }
            }
            else
            {
                action();
            }
        }

        private void _CapturePutVertex(ref VertexInfo vertexInfo)
        {
            if (_pspWavefrontObjWriter != null)
            {
                lock (this) _pspWavefrontObjWriter.PutVertex(ref vertexInfo);
            }
        }

        private static readonly GuPrimitiveType[] patch_prim_types = { GuPrimitiveType.TriangleStrip, GuPrimitiveType.LineStrip, GuPrimitiveType.Points };
        public override void DrawCurvedSurface(GlobalGpuState GlobalGpuState, GpuStateStruct GpuStateStruct, VertexInfo[,] Patch, int UCount, int VCount)
        {
            if (Patch == null) return;
            if (Patch.Length == 0) return;

            GpuState = GpuStateStruct;
            VertexType = GpuState.VertexState.Type;

            PrepareStateCommon(GpuState, ScaleViewport);
            PrepareStateDraw(GpuState);
            PrepareStateMatrix(GpuState, out _worldViewProjectionMatrix);

#if ENABLE_TEXTURES
            PrepareState_Texture_Common(GpuState);
            PrepareState_Texture_3D(GpuState);
#endif
            PrepareDrawStateFirst();

            int s_len = Patch.GetLength(0);
            int t_len = Patch.GetLength(1);

            if (s_len <= 1 || t_len <= 1)
            {
                // 无法构造三角形
                return;
            }

            float s_len_float = s_len;
            float t_len_float = t_len;

            var mipmap0 = GpuState.TextureMappingState.TextureState.Mipmap0;
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

        bool _doPrimStart;
        VertexTypeStruct _cachedVertexType;
        GuPrimitiveType _primitiveType;
        GLRenderTarget _logicOpsRenderTarget;

        public override void PrimStart(GlobalGpuState globalGpuState, GpuStateStruct gpuState, GuPrimitiveType primitiveType)
        {
            GpuState = gpuState;
            _primitiveType = primitiveType;
            _doPrimStart = true;
            ResetVertex();

            if (_shader != null)
            {
                _shader.GetUniform("lopEnabled").Set(gpuState.LogicalOperationState.Enabled);

                if (gpuState.LogicalOperationState.Enabled)
                {
                    if (_logicOpsRenderTarget == null)
                    {
                        _logicOpsRenderTarget = GLRenderTarget.Create(512, 272, RenderTargetLayers.Color);
                    }
                    GLRenderTarget.CopyFromTo(GLRenderTarget.Current, _logicOpsRenderTarget);

                    _shader.GetUniform("backtex").Set(GLTextureUnit.CreateAtIndex(1).SetFiltering(GLScaleFilter.Linear)
                        .SetWrap(GLWrap.ClampToEdge).SetTexture(_logicOpsRenderTarget.TextureColor));

                    _shader.GetUniform("lop").Set((int)gpuState.LogicalOperationState.Operation);

                    //new Bitmap(512, 272).SetChannelsDataInterleaved(LogicOpsRenderTarget.ReadPixels(), BitmapChannelList.RGBA).Save(@"c:\temp\test.png");
                }
            }
        }

        public override void PrimEnd()
        {
            EndVertex();
        }

        private void EndVertex()
        {
            //Console.Out.WriteLineColored(ConsoleColor.Green, $"DrawVertices Geometr: {_primitiveType.ToString()}");

            DrawVertices(ConvertGLGeometry(_primitiveType));

            ResetVertex();
        }

        public override void Prim(ushort vertexCount)
        {
            VertexType = GpuState.VertexState.Type;

            if (_doPrimStart || VertexType != _cachedVertexType)
            {
                _cachedVertexType = VertexType;
                _doPrimStart = false;

                PrepareStateCommon(GpuState, ScaleViewport);

                if (GpuState.ClearingMode)
                {
                    PrepareStateClear(GpuState);
                }
                else
                {
                    PrepareStateDraw(GpuState);
                }

                PrepareStateMatrix(GpuState, out _worldViewProjectionMatrix);

                PrepareDrawStateFirst();
            }

            uint morpingVertexCount, totalVerticesWithoutMorphing;

            PreparePrim(GpuState, out totalVerticesWithoutMorphing, vertexCount, out morpingVertexCount);

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
                                componentsOut[cc] += componentsIn[cc] * GpuState.MorphingState.MorphWeight(m);
                        }
                        verticesPtr[n].Normal = verticesPtr[n].Normal.Normalize();
                    }
                }
            }

            _CapturePrimitive(_primitiveType, GpuState.GetAddressRelativeToBaseOffset(GpuState.VertexAddress), vertexCount, ref VertexType, () =>
            {
                if (_indicesList.Length > 0)
                {
                    switch (_primitiveType)
                    {
                        case GuPrimitiveType.TriangleStrip:
                        case GuPrimitiveType.Sprites:
                            if (vertexCount > 0)
                            {
                                PutVertexIndexRelative(-1);
                                PutVertexIndexRelative(0);
                            }
                            break;
                        // Can't degenerate, flush.
                        default:
                            EndVertex();
                            break;
                    }
                }

                if (_primitiveType == GuPrimitiveType.Sprites)
                {
                    GL.glDisable(GL.GL_CULL_FACE);
                    for (var n = 0; n < vertexCount; n += 2)
                    {
                        VertexInfo v0, v1, v2, v3;

                        readVertex(n + 0, out v0);
                        readVertex(n + 1, out v3);

                        VertexUtils.GenerateTriangleStripFromSpriteVertices(ref v0, out v1, out v2, ref v3);

                        if (n > 0)
                        {
                            PutVertexIndexRelative(-1);
                            PutVertexIndexRelative(0);
                        }

                        PutVertices(v0, v1, v2, v3);
                    }
                }
                else
                {
                    VertexInfo VertexInfo;
                    for (var n = 0; n < vertexCount; n++)
                    {
                        readVertex(n, out VertexInfo);
                        PutVertex(VertexInfo);
                    }
                }
            });
        }

        private static GLGeometry ConvertGLGeometry(GuPrimitiveType primitiveType) => primitiveType switch
        {
            GuPrimitiveType.Lines => GLGeometry.GL_LINES,
            GuPrimitiveType.LineStrip => GLGeometry.GL_LINE_STRIP,
            GuPrimitiveType.Triangles => GLGeometry.GL_TRIANGLES,
            GuPrimitiveType.Points => GLGeometry.GL_POINTS,
            GuPrimitiveType.TriangleFan => GLGeometry.GL_TRIANGLE_FAN,
            GuPrimitiveType.TriangleStrip => GLGeometry.GL_TRIANGLE_STRIP,
            GuPrimitiveType.Sprites => GLGeometry.GL_TRIANGLE_STRIP,
            _ => throw new NotImplementedException("Not implemented PrimitiveType:'" + primitiveType + "'")
        };

        public override void BeforeDraw(GpuStateStruct gpuState)
        {
            RenderbufferManager.BindCurrentDrawBufferTexture(gpuState);
        }

        public override void DrawVideo(uint frameBufferAddress, OutputPixel* outputPixel, int width, int height)
        {
            RenderbufferManager.DrawVideo(frameBufferAddress, outputPixel, width, height);
        }

        public override void Finish(GpuStateStruct gpuState)
        {
        }

        public override void End(GpuStateStruct gpuState)
        {
            //PrepareWrite(GpuState);
        }

        public override void Sync(GpuStateStruct gpuState)
        {
        }

        public override void TextureFlush(GpuStateStruct gpuState)
        {
            TextureCache.RecheckAll();
        }

        public override void TextureSync(GpuStateStruct gpuState)
        {
        }

        public override void AddedDisplayList()
        {
        }

        public override void SetCurrent()
        {
            if (!IsCurrentWindow)
            {
                OpenglContext.MakeCurrent();
                IsCurrentWindow = true;
            }
        }

        public override void UnsetCurrent()
        {
            OpenglContext.ReleaseCurrent();
            IsCurrentWindow = false;
        }

        public static string GlGetString(int name) => GL.GetString(name);

        public override void InitSynchronizedOnce(IntPtr TargetHwnd)
        {
            //Memory.WriteBytesHook += OnMemoryWrite;
            ScaleViewport = PspStoredConfig.RenderScale;

            if (!AlreadyInitialized)
            {
                AlreadyInitialized = true;

                var completedEvent = new AutoResetEvent(false);

                new Thread(() =>
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

                    if (TargetHwnd == IntPtr.Zero)
                    {
                        Console.Out.WriteLineColored(ConsoleColor.White, $"## OpenGL Windowless Mode");
                        OpenglContext = GlContextFactory.CreateWindowless();
                    }
                    else
                    {
                        Console.Out.WriteLineColored(ConsoleColor.White, $"## OpenGL Window HWND: {TargetHwnd}");
                        OpenglContext = GlContextFactory.CreateFromWindowHandle(TargetHwnd);
                    }

                    OpenglContext.MakeCurrent();

                    try
                    {
                        Console.Out.WriteLineColored(ConsoleColor.White, "## OpenGL Context Version: {0}",
                            GlGetString(GL.GL_VERSION));
                        Console.Out.WriteLineColored(ConsoleColor.White, "## Depth Bits: {0}",
                            GL.glGetInteger(GL.GL_DEPTH_BITS));
                        Console.Out.WriteLineColored(ConsoleColor.White, "## Stencil Bits: {0}",
                            GL.glGetInteger(GL.GL_STENCIL_BITS));
                        Console.Out.WriteLineColored(ConsoleColor.White, "## Color Bits: {0},{1},{2},{3}",
                            GL.glGetInteger(GL.GL_RED_BITS), GL.glGetInteger(GL.GL_GREEN_BITS),
                            GL.glGetInteger(GL.GL_BLUE_BITS), GL.glGetInteger(GL.GL_ALPHA_BITS));

                        if (GL.glGetInteger(GL.GL_STENCIL_BITS) <= 0)
                        {
                            Console.Error.WriteLineColored(ConsoleColor.Red, "No stencil bits available!");
                            //throw new Exception("Couldn't initialize opengl");
                        }

                        OpenglContext.ReleaseCurrent();

                        completedEvent.Set();

                        Console.WriteLine("Opengl Initialize.");
                        try
                        {
                            while (Running)
                            {
                                Thread.Sleep(10);
                            }
                            StopEvent.Set();
                        }
                        finally
                        {
                            Console.WriteLine("Opengl Uninitialize.");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Opengl initialize Error: {0}", e);
                    }
                })
                {
                    Name = "GpuImplEventHandling",
                    IsBackground = true
                }.Start();

                completedEvent.WaitOne();
            }
        }

        public override void StopSynchronized()
        {
            //Running = false;
            //StopEvent.WaitOne();
            //NativeWindow.Dispose();
        }

        private void PrepareStateDraw(GpuStateStruct gpuState)
        {
            GL.glColorMask(true, true, true, true);

#if ENABLE_TEXTURES
            PrepareState_Texture_Common(gpuState);
#endif
            PrepareState_Blend(gpuState);
            PrepareState_Clip(gpuState);

            if (gpuState.VertexState.Type.Transform2D)
            {
                PrepareState_Colors_2D(gpuState);
                GL.glDisable(GL.GL_STENCIL_TEST);
                GL.glDisable(GL.GL_CULL_FACE);
                GL.DepthRange(0, 1);
                GL.glDisable(GL.GL_DEPTH_TEST);
                //GL.glDisable(EnableCap.Lighting);
            }
            else
            {
                PrepareState_Colors_3D(gpuState);
                PrepareState_CullFace(gpuState);
                PrepareState_Lighting(gpuState);
                PrepareState_Depth(gpuState);
                PrepareState_DepthTest(gpuState);
                PrepareState_Stencil(gpuState);
            }
            //GL.ShadeModel((GpuState->ShadeModel == ShadingModelEnum.Flat) ? ShadingModel.Flat : ShadingModel.Smooth);
            PrepareState_AlphaTest(gpuState);
        }

        private void PrepareState_Clip(GpuStateStruct gpuState)
        {
            if (!GL.EnableDisable(GL.GL_SCISSOR_TEST, gpuState.ClipPlaneState.Enabled))
            {
                return;
            }
            var scissor = gpuState.ClipPlaneState.Scissor;
            GL.glScissor(
                scissor.Left * ScaleViewport,
                scissor.Top * ScaleViewport,
                scissor.Width * ScaleViewport,
                scissor.Height * ScaleViewport
            );
        }

        private GL.DepthFunction DepthFunctionTranslate(int pspFunc)
        {
            return pspFunc switch
            {
                0 => GL.DepthFunction.Never,       // 永不通过
                1 => GL.DepthFunction.Less,        // 小于阈值
                2 => GL.DepthFunction.Equal,       // 等于阈值
                3 => GL.DepthFunction.Lequal,      // 小于等于阈值
                4 => GL.DepthFunction.Greater,     // 大于阈值
                5 => GL.DepthFunction.Notequal,    // 不等于阈值
                6 => GL.DepthFunction.Gequal,      // 大于等于阈值
                7 => GL.DepthFunction.Always,      // 始终通过
                _ => GL.DepthFunction.Always       // 未知值默认始终通过，避免渲染崩溃
            };
        }

        private void PrepareState_AlphaTest(GpuStateStruct gpuState)
        {
            if (!gpuState.AlphaTestState.Enabled)
            {
                GL.glDisable(GL.GL_ALPHA_TEST);
                return;
            }

            GL.glEnable(GL.GL_ALPHA_TEST);

            var glCompareFunc = DepthFunctionTranslate((int)gpuState.AlphaTestState.Function);

            float alphaThreshold = gpuState.AlphaTestState.Value / 255.0f;

            GL.glAlphaFunc((int)glCompareFunc, alphaThreshold);
        }

        private void PrepareState_Stencil(GpuStateStruct gpuState)
        {
            if (!GL.EnableDisable(GL.GL_STENCIL_TEST, gpuState.StencilState.Enabled))
            {
                return;
            }
            //if (state.stencilFuncFunc == 2) { outputDepthAndStencil(); assert(0); }
#if false
			Console.Error.WriteLine(
				"{0}:{1}:{2} - {3}, {4}, {5}",
				StencilFunctionTranslate[(int)GpuState->StencilState.Function],
				GpuState->StencilState.FunctionRef,
				GpuState->StencilState.FunctionMask,
				StencilOperationTranslate[(int)GpuState->StencilState.OperationFail],
				StencilOperationTranslate[(int)GpuState->StencilState.OperationZFail],
				StencilOperationTranslate[(int)GpuState->StencilState.OperationZPass]
			);
#endif
            GL.glStencilFunc(
                OpenglGpuImplConversionTables.StencilFunctionTranslate[(int)gpuState.StencilState.Function],
                gpuState.StencilState.FunctionRef,
                gpuState.StencilState.FunctionMask
            );

            GL.glStencilOp(
                OpenglGpuImplConversionTables.StencilOperationTranslate[(int)gpuState.StencilState.OperationFail],
                OpenglGpuImplConversionTables.StencilOperationTranslate[(int)gpuState.StencilState.OperationZFail],
                OpenglGpuImplConversionTables.StencilOperationTranslate[(int)gpuState.StencilState.OperationZPass]
            );
        }

        private void PrepareState_CullFace(GpuStateStruct gpuState)
        {
            if (!GL.EnableDisable(GL.GL_CULL_FACE, gpuState.BackfaceCullingState.Enabled))
            {
                return;
            }

            //GL.EnableDisable(EnableCap.CullFace, false);

            GL.glCullFace(gpuState.BackfaceCullingState.FrontFaceDirection == FrontFaceDirectionEnum.ClockWise
                ? GL.GL_FRONT
                : GL.GL_BACK);
        }

        private void PrepareState_Depth(GpuStateStruct gpuState)
        {
            GL.DepthRange(gpuState.DepthTestState.RangeNear, gpuState.DepthTestState.RangeFar);
        }

        private void PrepareState_DepthTest(GpuStateStruct gpuState)
        {
            if (gpuState.DepthTestState.Mask != 0 && gpuState.DepthTestState.Mask != 1)
            {
                Console.Error.WriteLine("WARNING! DepthTestState.Mask: {0}", gpuState.DepthTestState.Mask);
            }
            GL.glDepthMask(gpuState.DepthTestState.Mask == 0);
            if (!GL.EnableDisable(GL.GL_DEPTH_TEST, gpuState.DepthTestState.Enabled))
            {
                return;
            }
            GL.glDepthFunc(OpenglGpuImplConversionTables.DepthFunctionTranslate[(int)gpuState.DepthTestState.Function]);
        }

        private void PrepareState_Colors_2D(GpuStateStruct gpuState)
        {
            PrepareState_Colors_3D(gpuState);
        }

        private void PrepareState_Colors_3D(GpuStateStruct gpuState)
        {
            try
            {
                ShaderInfo.uniformColor.Set(gpuState.LightingState.AmbientModelColor.ToVector4());
            }
            catch
            {
            }

            // 对固定管线无害，在着色器管线中也不会破坏行为
            GL.EnableDisable(GL.GL_COLOR_MATERIAL, VertexType.HasColor);
        }

        private void PrepareState_Lighting(GpuStateStruct gpuState)
        {
            var lighting = gpuState.LightingState;

            if (!lighting.Enabled)
            {
                // 如果光照被禁用，使用环境色作为基础颜色
                ShaderInfo.uniformColor.NoWarning().Set(lighting.AmbientModelColor.ToVector4());
                return;
            }

            // 合成一个简单的基色 Ambient + Emissive + Diffuse（相加并 clamp 到 [0,1]）
            var ambient = lighting.AmbientModelColor.ToVector4();
            var emissive = lighting.EmissiveModelColor.ToVector4();
            var diffuse = lighting.DiffuseModelColor.ToVector4();

            var combined = new Vector4(
                MathF.Min(1f, ambient.X + emissive.X + diffuse.X),
                MathF.Min(1f, ambient.Y + emissive.Y + diffuse.Y),
                MathF.Min(1f, ambient.Z + emissive.Z + diffuse.Z),
                1f
            );

            ShaderInfo.uniformColor.NoWarning().Set(combined);

            // TODO: 固定功能的逐光源设置（glLight 等）未统一封装，

            //GL.LightModel(
            //	LightModelParameter.LightModelColorControl,
            //	(int)((LightingState->LightModel == LightModelEnum.SeparateSpecularColor) ? LightModelColorControl.SeparateSpecularColor : LightModelColorControl.SingleColor)
            //);
            //GL.LightModel(LightModelParameter.LightModelAmbient, &LightingState->AmbientLightColor.Red);
            //
            //for (int n = 0; n < 4; n++)
            //{
            //	var LightState = &(&LightingState->Light0)[n];
            //	LightName LightName = (LightName)(LightName.Light0 + n);
            //
            //	if (!GL.EnableDisable((EnableCap)(EnableCap.Light0 + n), LightState->Enabled))
            //	{
            //		continue;
            //	}
            //
            //	GL.Light(LightName, LightParameter.Specular, &LightState->SpecularColor.Red);
            //	GL.Light(LightName, LightParameter.Ambient, &LightState->AmbientColor.Red);
            //	GL.Light(LightName, LightParameter.Diffuse, &LightState->DiffuseColor.Red);
            //
            //	LightState->Position.W = 1.0f;
            //	GL.Light(LightName, LightParameter.Position, &LightState->Position.X);
            //
            //	GL.Light(LightName, LightParameter.ConstantAttenuation, &LightState->Attenuation.Constant);
            //	GL.Light(LightName, LightParameter.LinearAttenuation, &LightState->Attenuation.Linear);
            //	GL.Light(LightName, LightParameter.QuadraticAttenuation, &LightState->Attenuation.Quadratic);
            //
            //	if (LightState->Type == LightTypeEnum.SpotLight)
            //	{
            //		GL.Light(LightName, LightParameter.SpotDirection, &LightState->SpotDirection.X);
            //		GL.Light(LightName, LightParameter.SpotExponent, &LightState->SpotExponent);
            //		GL.Light(LightName, LightParameter.SpotCutoff, &LightState->SpotCutoff);
            //	}
            //	else
            //	{
            //		GL.Light(LightName, LightParameter.SpotExponent, 0);
            //		GL.Light(LightName, LightParameter.SpotCutoff, 180);
            //	}
            //}
        }

        private void PrepareState_Blend(GpuStateStruct gpuState)
        {
            var blendingState = gpuState.BlendingState;
            if (!GL.EnableDisable(GL.GL_BLEND, blendingState.Enabled))
            {
                return;
            }

            //Console.WriteLine("Blend!");

            var openglFunctionSource = OpenglGpuImplConversionTables.BlendFuncSrcTranslate[(int)blendingState.FunctionSource];
            //var OpenglFunctionDestination = BlendFuncDstTranslate[(int)BlendingState->FunctionDestination];
            var openglFunctionDestination = OpenglGpuImplConversionTables.BlendFuncSrcTranslate[(int)blendingState.FunctionDestination];

            Func<ColorfStruct, int> getBlendFix = (color) =>
            {
                if (color.IsColorf(0, 0, 0)) return GL.GL_ZERO;
                if (color.IsColorf(1, 1, 1)) return GL.GL_ONE;
                return GL.GL_CONSTANT_COLOR;
            };

            if (blendingState.FunctionSource == GuBlendingFactorSource.GuFix)
            {
                openglFunctionSource = getBlendFix(blendingState.FixColorSource);
            }

            if (blendingState.FunctionDestination == GuBlendingFactorDestination.GuFix)
            {
                if ((int)openglFunctionSource == GL.GL_CONSTANT_COLOR && (blendingState.FixColorSource + blendingState.FixColorDestination).IsColorf(1, 1, 1))
                {
                    openglFunctionDestination = GL.GL_ONE_MINUS_CONSTANT_COLOR;
                }
                else
                {
                    openglFunctionDestination = getBlendFix(blendingState.FixColorDestination);
                }
            }
            //Console.WriteLine("{0}, {1}", OpenglFunctionSource, OpenglFunctionDestination);

            var openglBlendEquation = OpenglGpuImplConversionTables.BlendEquationTranslate[(int)blendingState.Equation];

            /*
            Console.WriteLine(
                "{0} : {1} -> {2}",
                OpenglBlendEquation, OpenglFunctionSource, OpenglFunctionDestination
            );
            */

            GL.glBlendEquation(openglBlendEquation);

            GL.glBlendFunc(openglFunctionSource, openglFunctionDestination);

            GL.glBlendColor(
                blendingState.FixColorDestination.Red,
                blendingState.FixColorDestination.Green,
                blendingState.FixColorDestination.Blue,
                blendingState.FixColorDestination.Alpha
            );
        }

        private void PrepareState_Texture_2D(GpuStateStruct gpuState)
        {
            var textureMappingState = gpuState.TextureMappingState;
            var mipmap0 = textureMappingState.TextureState.Mipmap0;

            if (textureMappingState.Enabled)
            {
                _textureMatrix = Matrix4x4.CreateScale(
                        1.0f / mipmap0.BufferWidth,
                        1.0f / mipmap0.TextureHeight,
                        1.0f
                );

                //GL.glActiveTexture(TextureUnit.Texture0);
                //GL.glMatrixMode(MatrixMode.Texture);
                //GL.glLoadIdentity();
                //
                //GL.glScale(
                //	1.0f / Mipmap0->BufferWidth,
                //	1.0f / Mipmap0->TextureHeight,
                //	1.0f
                //);
            }
        }

        private void PrepareState_Texture_3D(GpuStateStruct gpuState)
        {
            var textureMappingState = gpuState.TextureMappingState;
            var textureState = textureMappingState.TextureState;

            if (textureMappingState.Enabled)
            {
                _textureMatrix = Matrix4x4.Identity;

                switch (textureMappingState.TextureMapMode)
                {
                    case TextureMapMode.GuTextureCoords:

                        _textureMatrix = _textureMatrix *
                                         Matrix4x4.CreateTranslation(textureState.OffsetU, textureState.OffsetV, 0) *
                                         Matrix4x4.CreateScale(textureState.ScaleU, textureState.ScaleV, 1);
                        break;
                    case TextureMapMode.GuTextureMatrix:
                        switch (gpuState.TextureMappingState.TextureProjectionMapMode)
                        {
                            default:
                                Console.Error.WriteLine("NotImplemented: GU_TEXTURE_MATRIX: {0}", gpuState.TextureMappingState.TextureProjectionMapMode);
                                break;
                        }
                        break;
                    case TextureMapMode.GuEnvironmentMap:
                        Console.Error.WriteLine("NotImplemented: GU_ENVIRONMENT_MAP");
                        break;
                    default:
                        Console.Error.WriteLine("NotImplemented TextureMappingState->TextureMapMode: " + textureMappingState.TextureMapMode);
                        break;
                }
            }
        }

        private void PrepareState_Texture_Common(GpuStateStruct gpuState)
        {
            var textureMappingState = gpuState.TextureMappingState;
            //var ClutState = TextureMappingState.ClutState;
            var textureState = textureMappingState.TextureState;

            if (!GL.EnableDisable(GL.GL_TEXTURE_2D, textureMappingState.Enabled)) return;

            if (VertexType.Transform2D)
            {
                PrepareState_Texture_2D(gpuState);
            }
            else
            {
                PrepareState_Texture_3D(gpuState);
            }

            //GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            //glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

            RenderbufferManager.TextureCacheGetAndBind(gpuState);

            //CurrentTexture.Save("test.png");

            //GL.glTexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvModeTranslate[(int)TextureState.Effect]);
        }

        public static void PrepareStateCommon(GpuStateStruct gpuState, int scaleViewport)
        {
            var viewport = gpuState.Viewport;

            // PSP 中 viewport 坐标通常以左上为原点，OpenGL 以左下为原点
            // 简单映射
            var left = (int)viewport.RegionTopLeft.X * scaleViewport;
            var top = (int)viewport.RegionTopLeft.Y * scaleViewport;
            var width = Math.Max(1, (int)viewport.RegionSize.X * scaleViewport);
            var height = Math.Max(1, (int)viewport.RegionSize.Y * scaleViewport);

            GL.glViewport(left, top, width, height);

            GL.glDisable(GL.GL_LIGHTING);
            GL.glDisable(GL.GL_POLYGON_OFFSET_FILL);

            try
            {
                GL.EnableDisable(GL.GL_DITHER, true);
            }
            catch
            {
            }
        }

        public static void PrepareStateMatrix(GpuStateStruct gpuState, out Matrix4x4 worldViewProjectionMatrix)
        {
            if (gpuState.VertexState.Type.Transform2D)
            {
                // OrthographicOffCenter(左, 右, 下, 上, 近裁面, 远裁面)
                worldViewProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
                    0, 480,    // 左右边界（宽度480）
                    272, 0,    // 下上边界（翻转Y轴，匹配PSP左上角原点）
                    0, 0xFFFF  // 深度范围（0~65535，正序）
                );
            }
            else
            {
                worldViewProjectionMatrix =
                    gpuState.VertexState.WorldMatrix * gpuState.VertexState.ViewMatrix *
                    gpuState.VertexState.ProjectionMatrix;
            }
        }

        public static void PrepareStateClear(GpuStateStruct gpuState)
        {
            bool colorMask = false, alphaMask = false;
            bool depthMask = gpuState.ClearFlags.HasFlag(ClearBufferSet.DepthBuffer);
            bool stencilMask = gpuState.ClearFlags.HasFlag(ClearBufferSet.StencilBuffer);

            GL.glDisable(GL.GL_BLEND);
            GL.glDisable(GL.GL_LIGHTING);
            GL.glDisable(GL.GL_TEXTURE_2D);
            GL.glDisable(GL.GL_ALPHA_TEST);
            GL.glDisable(GL.GL_DEPTH_TEST);
            GL.glDisable(GL.GL_STENCIL_TEST);
            GL.glDisable(GL.GL_FOG);
            GL.glDisable(GL.GL_LOGIC_OP);
            GL.glDisable(GL.GL_CULL_FACE);
            GL.glDepthMask(false);

            if (gpuState.ClearFlags.HasFlag(ClearBufferSet.ColorBuffer))
            {
                colorMask = true;
            }

            if (GL.EnableDisable(GL.GL_STENCIL_TEST, stencilMask))
            {
                alphaMask = true;
                GL.glStencilFunc(GL.GL_ALWAYS, 0x00, 0xFF);
                GL.glStencilOp(GL.GL_REPLACE, GL.GL_REPLACE, GL.GL_REPLACE);
                GL.glStencilMask(0xFF);
            }

            if (depthMask)
            {
                GL.glEnable(GL.GL_DEPTH_TEST);
                GL.glDepthFunc(GL.GL_ALWAYS);
                GL.glDepthMask(true);
                GL.glDepthRange(0.0f, 0.0f);
                //GL.glDepthRange(0.0f, 1.0f); // Original value
            }

            GL.glColorMask(colorMask, colorMask, colorMask, alphaMask);

            GL.glClearDepthf(1.0f);

            uint clearBits = 0;
            if (colorMask) clearBits |= GL.GL_COLOR_BUFFER_BIT;
            if (depthMask) clearBits |= GL.GL_DEPTH_BUFFER_BIT;
            if (stencilMask) clearBits |= GL.GL_STENCIL_BUFFER_BIT;
            if (clearBits != 0)
            {
                GL.glClear(clearBits);
            }
        }

        private void TransferToFrameBuffer(GpuStateStruct gpuState)
        {
            var textureTransferState = gpuState.TextureTransferState;

            var sourceX = textureTransferState.SourceX;
            var sourceY = textureTransferState.SourceY;
            var destinationX = textureTransferState.DestinationX;
            var destinationY = textureTransferState.DestinationY;
            var bytesPerPixel = textureTransferState.BytesPerPixel;

            var drawBuffer = gpuState.DrawBufferState;

            // 如果目的 DrawBuffer 的行宽或每像素大小和传输描述不一致，退回到通用实现以保证正确性
            if (bytesPerPixel != drawBuffer.BytesPerPixel || textureTransferState.DestinationLineWidth != drawBuffer.Width)
            {
                TransferGeneric(gpuState);
                return;
            }

            // 计算缓冲区总大小以供安全访问（以行宽计算）
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

            // 逐行拷贝，注意源/目的可能有不同的行首偏移
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
            // 完成后，不在此强制刷新 GPU 纹理缓存；上层或 RenderbufferManager 会在需要时使用 DrawBuffer 数据。
        }

        private void TransferGeneric(GpuStateStruct gpuState)
        {
            var textureTransferState = gpuState.TextureTransferState;

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

        public override void Transfer(GpuStateStruct gpuState)
        {
            var textureTransferState = gpuState.TextureTransferState;

            // 如果写入目标是当前 DrawBuffer（地址和行宽与 DrawBuffer 匹配），走专用路径以获得更高效的拷贝
            if (
                textureTransferState.DestinationAddress.Address == gpuState.DrawBufferState.Address &&
                textureTransferState.DestinationLineWidth == gpuState.DrawBufferState.Width &&
                textureTransferState.BytesPerPixel == gpuState.DrawBufferState.BytesPerPixel
            )
            {
                TransferToFrameBuffer(gpuState);
            }
            else
            {
                TransferGeneric(gpuState);
            }
        }

        /*
        readonly byte[] TempBuffer = new byte[512 * 512 * 4];

        struct GlPixelFormat {
            PixelFormats pspFormat;
            float size;
            uint  internal;
            uint  external;
            uint  opengl;
            uint  isize() { return cast(uint)size; }
        }

        static const auto GlPixelFormats = [
            GlPixelFormat(PixelFormats.GU_PSM_5650,   2, 3, GL_RGB,  GL_UNSIGNED_SHORT_5_6_5_REV),
            GlPixelFormat(PixelFormats.GU_PSM_5551,   2, 4, GL_RGBA, GL_UNSIGNED_SHORT_1_5_5_5_REV),
            GlPixelFormat(PixelFormats.GU_PSM_4444,   2, 4, GL_RGBA, GL_UNSIGNED_SHORT_4_4_4_4_REV),
            GlPixelFormat(PixelFormats.GU_PSM_8888,   4, 4, GL_RGBA, GL_UNSIGNED_INT_8_8_8_8_REV),
            GlPixelFormat(PixelFormats.GU_PSM_T4  , 0.5, 1, GL_COLOR_INDEX, GL_COLOR_INDEX4_EXT),
            GlPixelFormat(PixelFormats.GU_PSM_T8  ,   1, 1, GL_COLOR_INDEX, GL_COLOR_INDEX8_EXT),
            GlPixelFormat(PixelFormats.GU_PSM_T16 ,   2, 4, GL_COLOR_INDEX, GL_COLOR_INDEX16_EXT),
            GlPixelFormat(PixelFormats.GU_PSM_T32 ,   4, 4, GL_RGBA, GL_UNSIGNED_INT ), // COLOR_INDEX, GL_COLOR_INDEX32_EXT Not defined.
            GlPixelFormat(PixelFormats.GU_PSM_DXT1,   4, 4, GL_RGBA, GL_COMPRESSED_RGBA_S3TC_DXT1_EXT),
            GlPixelFormat(PixelFormats.GU_PSM_DXT3,   4, 4, GL_RGBA, GL_COMPRESSED_RGBA_S3TC_DXT3_EXT),
            GlPixelFormat(PixelFormats.GU_PSM_DXT5,   4, 4, GL_RGBA, GL_COMPRESSED_RGBA_S3TC_DXT5_EXT),
        ];
        */

        //[HandleProcessCorruptedStateExceptions]
        //private void PrepareRead(GpuStateStruct* GpuState)
        //{
        //	if (true)
        //	{
        //		var GlPixelFormat = GlPixelFormatList[(int)GpuState->DrawBufferState.Format];
        //		int Width = (int)GpuState->DrawBufferState.Width;
        //		if (Width == 0) Width = 512;
        //		int Height = 272;
        //		int ScanWidth = PixelFormatDecoder.GetPixelsSize(GlPixelFormat.GuPixelFormat, Width);
        //		int PixelSize = PixelFormatDecoder.GetPixelsSize(GlPixelFormat.GuPixelFormat, 1);
        //		//GpuState->DrawBufferState.Format
        //		var Address = (void*)Memory.PspAddressToPointerSafe(GpuState->DrawBufferState.Address, 0);
        //		GL.PixelStore(PixelStoreParameter.PackAlignment, PixelSize);
        //		//Console.WriteLine("PrepareRead: {0:X}", Address);
        //
        //		try
        //		{
        //			GL.WindowPos2(0, 272);
        //			GL.PixelZoom(1, -1);
        //
        //			GL.DrawPixels(Width, Height, PixelFormat.Rgba, GlPixelFormat.OpenglPixelType, new IntPtr(Address));
        //			//GL.DrawPixels(512, 272, PixelFormat.AbgrExt, PixelType.UnsignedInt8888, new IntPtr(Memory.PspAddressToPointerSafe(Address)));
        //
        //			//GL.WindowPos2(0, 0);
        //			//GL.PixelZoom(1, 1);
        //		}
        //		catch (Exception Exception)
        //		{
        //			Console.WriteLine(Exception);
        //		}
        //	}
        //}

        //int[] pboIds = { -1 };
        //
        //static bool UsePbo = false;
        //
        //private void PreParePbos()
        //{
        //	if (UsePbo)
        //	{
        //		if (pboIds[0] == -1)
        //		{
        //			GL.GenBuffers(1, pboIds);
        //			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, pboIds[0]);
        //			GL.BufferData(BufferTarget.PixelUnpackBuffer, new IntPtr(512 * 272 * 4), IntPtr.Zero, BufferUsageHint.StreamRead);
        //			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
        //		}
        //		GL.BindBuffer(BufferTarget.PixelPackBuffer, pboIds[0]);
        //	}
        //}
        //
        //private void UnPreParePbos()
        //{
        //	if (UsePbo)
        //	{
        //		GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        //	}
        //}

        //private void SaveFrameBuffer(GpuStateStruct* GpuState, string FileName)
        //{
        //	var GlPixelFormat = GlPixelFormatList[(int)GuPixelFormats.RGBA_8888];
        //	int Width = (int)GpuState->DrawBufferState.Width;
        //	if (Width == 0) Width = 512;
        //	int Height = 272;
        //	int ScanWidth = PixelFormatDecoder.GetPixelsSize(GlPixelFormat.GuPixelFormat, Width);
        //	int PixelSize = PixelFormatDecoder.GetPixelsSize(GlPixelFormat.GuPixelFormat, 1);
        //
        //	if (Width == 0) Width = 512;
        //
        //	GL.PixelStore(PixelStoreParameter.PackAlignment, PixelSize);
        //
        //	var FB = new Bitmap(Width, Height);
        //	var Data = new byte[Width * Height * 4];
        //
        //	fixed (byte* DataPtr = Data)
        //	{
        //		//glBindBufferARB(GL_PIXEL_PACK_BUFFER_ARB, pboIds[index]);
        //		GL.ReadPixels(0, 0, Width, Height, PixelFormat.Rgba, GlPixelFormat.OpenglPixelType, new IntPtr(DataPtr));
        //
        //		BitmapUtils.TransferChannelsDataInterleaved(
        //			FB.GetFullRectangle(),
        //			FB,
        //			DataPtr,
        //			BitmapUtils.Direction.FromDataToBitmap,
        //			BitmapChannel.Red,
        //			BitmapChannel.Green,
        //			BitmapChannel.Blue,
        //			BitmapChannel.Alpha
        //		);
        //	}
        //
        //	FB.Save(FileName);
        //}

        //[HandleProcessCorruptedStateExceptions]
        //private void PrepareWrite(GpuStateStruct* GpuState)
        //{
        //	//GL.Flush();
        //	//return;
        //
        //#if true
        //	//if (SwapBuffers)
        //	//{
        //	//	RenderGraphicsContext.SwapBuffers();
        //	//}
        //	//
        //	//GL.PushAttrib(AttribMask.EnableBit);
        //	//GL.PushAttrib(AttribMask.TextureBit);
        //	//{
        //	//	GL.Enable(EnableCap.Texture2D);
        //	//	GL.BindTexture(TextureTarget.Texture2D, FrameBufferTexture);
        //	//	{
        //	//		//GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, 512, 272);
        //	//		GL.CopyTexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 0, 0, 512, 272, 0);
        //	//		//GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, new uint[] { 0xFFFF00FF });
        //	//	}
        //	//	GL.BindTexture(TextureTarget.Texture2D, 0);
        //	//}
        //	//GL.PopAttrib();
        //	//GL.PopAttrib();
        //#else
        //
        //	//Console.WriteLine("PrepareWrite");
        //	try
        //	{
        //		var GlPixelFormat = GlPixelFormatList[(int)GpuState->DrawBufferState.Format];
        //		int Width = (int)GpuState->DrawBufferState.Width;
        //		if (Width == 0) Width = 512;
        //		int Height = 272;
        //		int ScanWidth = PixelFormatDecoder.GetPixelsSize(GlPixelFormat.GuPixelFormat, Width);
        //		int PixelSize = PixelFormatDecoder.GetPixelsSize(GlPixelFormat.GuPixelFormat, 1);
        //		//GpuState->DrawBufferState.Format
        //		var Address = (void*)Memory.PspAddressToPointerSafe(GpuState->DrawBufferState.Address);
        //
        //		//Console.WriteLine("{0}", GlPixelFormat.GuPixelFormat);
        //
        //		//Console.WriteLine("{0:X}", GpuState->DrawBufferState.Address);
        //		GL.PixelStore(PixelStoreParameter.PackAlignment, PixelSize);
        //
        //		fixed (void* _TempBufferPtr = &TempBuffer[0])
        //		{
        //			var Input = (byte*)_TempBufferPtr;
        //			var Output = (byte*)Address;
        //
        //			PreParePbos();
        //			if (this.pboIds[0] > 0)
        //			{
        //				GL.ReadPixels(0, 0, Width, Height, PixelFormat.Rgba, GlPixelFormat.OpenglPixelType, IntPtr.Zero);
        //				Input = (byte*)GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly).ToPointer();
        //				GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
        //				if (Input == null)
        //				{
        //					Console.WriteLine("PBO ERROR!");
        //				}
        //			}
        //			else
        //			{
        //				GL.ReadPixels(0, 0, Width, Height, PixelFormat.Rgba, GlPixelFormat.OpenglPixelType, new IntPtr(_TempBufferPtr));
        //			}
        //			UnPreParePbos();
        //
        //			for (int Row = 0; Row < Height; Row++)
        //			{
        //				var ScanIn = (byte*)&Input[ScanWidth * Row];
        //				var ScanOut = (byte*)&Output[ScanWidth * (Height - Row - 1)];
        //				//Console.WriteLine("{0}:{1},{2},{3}", Row, PixelSize, Width, ScanWidth);
        //				PointerUtils.Memcpy(ScanOut, ScanIn, ScanWidth);
        //			}
        //		}
        //	}
        //	catch (Exception Exception)
        //	{
        //		Console.WriteLine(Exception);
        //	}
        //
        //	if (SwapBuffers)
        //	{
        //		RenderGraphicsContext.SwapBuffers();
        //	}
        // #endif
        //}
    }
}