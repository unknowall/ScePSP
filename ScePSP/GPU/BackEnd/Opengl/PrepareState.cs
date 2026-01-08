using LightGL;
using ScePSP.Core.GpuBackEnd.State;
using ScePSP.Utils;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ScePSP.Core.GpuBackEnd.OpenGL
{
    public unsafe partial class OpenglBackEnd : GpuBackEnd
    {
        public enum TexCoordMode
        {
            Default = 0,
            GU_ENV_MAP = 1,
            GU_POSITION = 2,
            GU_NORMAL = 3,
            GU_NORMALIZED_NORMAL = 4,
            GU_UV = 5,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct LightSubSet
        {
            public Vector4 ambient;       // Offset: 0, Size: 16
            public Vector4 diffuse;       // Offset: 16, Size: 16
            public Vector4 specular;      // Offset: 32, Size: 16
            public Vector4 position;      // Offset: 48, Size: 16 (w=0: Dir, w=1: Point/Spot)

            public Vector3 spotDirection; // Offset: 64, Size: 12
            public float spotExponent;    // Offset: 76, Size: 4  (Total 16 bytes aligned)

            public float spotCutoff;      // Offset: 80, Size: 4
            public float constantAttenuation; // Offset: 84, Size: 4
            public float linearAttenuation;    // Offset: 88, Size: 4
            public float quadraticAttenuation; // Offset: 92, Size: 4 (Total 16 bytes aligned)

            public int enabled;           // Offset: 96, Size: 4 (GLSL bool is 4 bytes in UBO)
            public int type;              // Offset: 100, Size: 4

            // Padding to align struct size to multiple of 16 (std140 rule)
            // Current size: 104 bytes. Next 16-byte boundary: 112 bytes.
            private int _padding1;        // Offset: 104, Size: 4
            private int _padding2;        // Offset: 108, Size: 4
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct LightSet
        {
            public Vector4 materialEmission;    // Offset: 0, Size: 16
            public Vector4 materialAmbient;     // Offset: 16, Size: 16
            public Vector4 materialDiffuse;     // Offset: 32, Size: 16
            public Vector4 materialSpecular;    // Offset: 48, Size: 16
            public float materialShininess;     // Offset: 64, Size: 4

            // Padding to align lightModelAmbient (vec4) to 16 bytes
            // Current Offset: 68. Target: 80.
            private int _pad1;                  // Offset: 68, Size: 4
            private int _pad2;                  // Offset: 72, Size: 4
            private int _pad3;                  // Offset: 76, Size: 4

            public Vector4 lightModelAmbient;   // Offset: 80, Size: 16
            public int lightModelColorControl;  // Offset: 96, Size: 4
            public int MaterialColorComponents; // Offset: 100, Size: 4

            // Padding to align Lights array start to 16 bytes
            // Current Offset: 104. Target: 112.
            private int _pad4;                  // Offset: 104, Size: 4
            private int _pad5;                  // Offset: 108, Size: 4

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public LightSubSet[] Lights;        // Offset: 112, Size: 112 * 4 = 448

            public LightSet()
            {
                // Initialize array to avoid null reference
                Lights = new LightSubSet[4];
            }
        }

        LightSet LightData = new LightSet();

        private DepthFunction DepthFunctionTranslate(int pspFunc)
        {
            return pspFunc switch
            {
                0 => DepthFunction.Never,       // 永不通过
                1 => DepthFunction.Less,        // 小于阈值
                2 => DepthFunction.Equal,       // 等于阈值
                3 => DepthFunction.Lequal,      // 小于等于阈值
                4 => DepthFunction.Greater,     // 大于阈值
                5 => DepthFunction.Notequal,    // 不等于阈值
                6 => DepthFunction.Gequal,      // 大于等于阈值
                7 => DepthFunction.Always,      // 始终通过
                _ => DepthFunction.Always       // 未知值默认始终通过，避免渲染崩溃
            };
        }

        public void PrepareStateCommon(GpuStateStruct gpuState, int scaleViewport)
        {
            if (_shader == null) DrawInitVertices();

            var viewport = gpuState.Viewport;

            // PSP 中 viewport 坐标通常以左上为原点，OpenGL 以左下为原点
            var left = (int)viewport.RegionTopLeft.X * scaleViewport;
            var top = (int)viewport.RegionTopLeft.Y * scaleViewport;
            var width = Math.Max(1, (int)viewport.RegionSize.X * scaleViewport);
            var height = Math.Max(1, (int)viewport.RegionSize.Y * scaleViewport);

            GL.Viewport(left, top, width, height);

            //GL.Disable(GL.GL_LIGHTING);
            //GL.Disable(GL.GL_POLYGON_OFFSET_FILL);
            //GL.EnableDisable(GL.GL_DITHER, true);

        }

        private void PrepareShaderTexSet()
        {
            var vertexType = GpuState.VertexState.Type;

            ShaderInfo.matrixWorldViewProjection.Set(_worldViewProjectionMatrix);
            ShaderInfo.matrixWorld.Set(GpuState.VertexState.WorldMatrix);
            ShaderInfo.matrixView.Set(GpuState.VertexState.ViewMatrix);

            ShaderInfo.matrixTexture.Set(_textureMatrix);

            ShaderInfo.uniformColor.NoWarning().Set(GpuState.LightingState.AmbientModelColor.ToVector4());

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
                    ShaderInfo.matrixBones.Set(bones);
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

        private void PrepareStateDraw(GpuStateStruct gpuState)
        {
            GL.ColorMask(true, true, true, true);

            PrepareState_Texture_Common(gpuState);
            PrepareState_Blend(gpuState);
            PrepareState_Clip(gpuState);

            if (gpuState.VertexState.Type.Transform2D)
            {
                PrepareState_Colors_2D(gpuState);
                GL.Disable(GL.GL_STENCIL_TEST);
                GL.Disable(GL.GL_CULL_FACE);
                GL.DepthRange(0, 1);
                GL.Disable(GL.GL_DEPTH_TEST);
                GL.Disable(GL.GL_LIGHTING);
                //PrepareState_Lighting(gpuState);
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
            //GL.ShadeMode1((GpuState.ShadeModel == ShadingModelEnum.Flat) ? ShadingModel.Flat : ShadingModel.Smooth);
            PrepareState_AlphaTest(gpuState);
        }

        private void PrepareState_Clip(GpuStateStruct gpuState)
        {
            if (!GL.EnableDisable(GL.GL_SCISSOR_TEST, gpuState.ClipPlaneState.Enabled))
            {
                return;
            }
            var scissor = gpuState.ClipPlaneState.Scissor;
            GL.Scissor(
                scissor.Left * ScaleViewport,
                scissor.Top * ScaleViewport,
                scissor.Width * ScaleViewport,
                scissor.Height * ScaleViewport
            );
        }

        private void PrepareState_AlphaTest(GpuStateStruct gpuState)
        {
            if (!gpuState.AlphaTestState.Enabled)
            {
                GL.Disable(GL.GL_ALPHA_TEST);
                return;
            }

            GL.Enable(GL.GL_ALPHA_TEST);

            var glCompareFunc = DepthFunctionTranslate((int)gpuState.AlphaTestState.Function);

            float alphaThreshold = gpuState.AlphaTestState.Value / 255.0f;

            GL.AlphaFunc((int)glCompareFunc, alphaThreshold);
        }

        private void PrepareState_Stencil(GpuStateStruct gpuState)
        {
            if (!GL.EnableDisable(GL.GL_STENCIL_TEST, gpuState.StencilState.Enabled))
            {
                return;
            }

            //if (state.stencilFuncFunc == 2) { outputDepthAndStencil(); assert(0); }

            //Console.Error.WriteLine(
            //    "{0}:{1}:{2} - {3}, {4}, {5}",
            //    OpenglGpuImplConversionTables.StencilFunctionTranslate[(int)GpuState.StencilState.Function],
            //    GpuState.StencilState.FunctionRef,
            //    GpuState.StencilState.FunctionMask,
            //    OpenglGpuImplConversionTables.StencilOperationTranslate[(int)GpuState.StencilState.OperationFail],
            //    OpenglGpuImplConversionTables.StencilOperationTranslate[(int)GpuState.StencilState.OperationZFail],
            //    OpenglGpuImplConversionTables.StencilOperationTranslate[(int)GpuState.StencilState.OperationZPass]
            //);

            GL.StencilFunc(
                OpenglGpuImplConversionTables.StencilFunctionTranslate[(int)gpuState.StencilState.Function],
                gpuState.StencilState.FunctionRef,
                gpuState.StencilState.FunctionMask
            );

            GL.StencilOp(
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

            var face = gpuState.BackfaceCullingState.FrontFaceDirection == FrontFaceDirectionEnum.ClockWise
                ? GL.GL_FRONT
                : GL.GL_BACK;

            //if(face == GL.GL_FRONT)
            //{
            //    ShaderInfo.hasReversedNormal.Set(true);
            //}
            //else
            //{
            //    ShaderInfo.hasReversedNormal.Set(false);
            //}

            GL.CullFace(face);
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
            GL.DepthMask(gpuState.DepthTestState.Mask == 0);
            if (!GL.EnableDisable(GL.GL_DEPTH_TEST, gpuState.DepthTestState.Enabled))
            {
                return;
            }
            GL.DepthFunc(OpenglGpuImplConversionTables.DepthFunctionTranslate[(int)gpuState.DepthTestState.Function]);
        }

        private void PrepareState_Colors_2D(GpuStateStruct gpuState)
        {
            PrepareState_Colors_3D(gpuState);
        }

        private void PrepareState_Colors_3D(GpuStateStruct gpuState)
        {
            GL.EnableDisable(GL.GL_COLOR_MATERIAL, VertexType.HasColor);
        }

        private void PrepareState_Lighting(GpuStateStruct gpuState)
        {
            var lighting = gpuState.LightingState;

            if (!lighting.Enabled)
            {
                ShaderInfo.lightenable.Set(false);
                //如果光照被禁用，使用环境色作为基础颜色
                ShaderInfo.uniformColor.Set(lighting.AmbientModelColor.ToVector4());
                return;
            }

            ShaderInfo.lightenable.Set(true);

            const int LIGHT_MODEL_COLOR_CONTROL_SEPARATE_SPECULAR_COLOR = 1;
            const int LIGHT_MODEL_COLOR_CONTROL_SINGLE_COLOR = 0;

            LightData.lightModelColorControl = lighting.LightModel == LightModelEnum.SeparateSpecularColor
                ? LIGHT_MODEL_COLOR_CONTROL_SEPARATE_SPECULAR_COLOR
                : LIGHT_MODEL_COLOR_CONTROL_SINGLE_COLOR;

            LightData.lightModelAmbient = lighting.AmbientLightColor.ToVector4();

            LightData.materialAmbient = lighting.AmbientModelColor.ToVector4();
            LightData.materialEmission = lighting.EmissiveModelColor.ToVector4();
            LightData.materialDiffuse = lighting.DiffuseModelColor.ToVector4();
            if (LightData.materialDiffuse.X == 0 && LightData.materialDiffuse.Y == 0 && LightData.materialDiffuse.Z == 0){
                LightData.materialDiffuse = new Vector4(1, 1, 1, 1);
            }
            LightData.materialSpecular = lighting.SpecularModelColor.ToVector4();
            LightData.materialShininess = Math.Clamp(lighting.SpecularPower, 1f, 128f);

            //Ambient = 1, Diffuse = 2, Specular = 4,
            //mbientAndDiffuse = Ambient | Diffuse,
            //DiffuseAndSpecular = Diffuse | Specular,
            //All = Ambient | DiffuseAndSpecular,
            LightData.MaterialColorComponents = (int)lighting.MaterialColorComponents;

            for (byte n = 0; n < 4; n++)
            {
                var light = lighting.Light(n);

                LightData.Lights[n].enabled = light.Enabled ? 1 : 0;

                if (!light.Enabled) continue;

                LightData.Lights[n].type = (int)light.Type; //Directional = 0, PointLight = 1, SpotLight = 2,

                LightData.Lights[n].ambient = light.AmbientColor.ToVector4();

                LightData.Lights[n].diffuse = light.DiffuseColor.ToVector4();

                LightData.Lights[n].position = light.Position.ToVector4();

                LightData.Lights[n].specular = light.SpecularColor.ToVector4();

                LightData.Lights[n].spotDirection = light.SpotDirection.ToRVector3();

                LightData.Lights[n].spotExponent = light.SpotExponent;

                LightData.Lights[n].spotCutoff = light.SpotCutoff;

                LightData.Lights[n].constantAttenuation = light.Attenuation.Constant;

                LightData.Lights[n].linearAttenuation = light.Attenuation.Linear;

                LightData.Lights[n].quadraticAttenuation = light.Attenuation.Quadratic;
            }

            _lightBuf.SetStructData<LightSet>(LightData);
        }

        private void PrepareState_Blend(GpuStateStruct gpuState)
        {
            var blendingState = gpuState.BlendingState;
            if (!GL.EnableDisable(GL.GL_BLEND, blendingState.Enabled))
            {
                return;
            }

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

            //Console.WriteLine("PrepareState_Blend {0}, {1}", OpenglFunctionSource, OpenglFunctionDestination);

            var openglBlendEquation = OpenglGpuImplConversionTables.BlendEquationTranslate[(int)blendingState.Equation];

            //Console.WriteLine("PrepareState_Blend {0} : {1} -> {2}", OpenglBlendEquation, OpenglFunctionSource, OpenglFunctionDestination);

            GL.BlendEquation(openglBlendEquation);

            GL.BlendFunc(openglFunctionSource, openglFunctionDestination);

            GL.BlendColor(
                blendingState.FixColorDestination.Red,
                blendingState.FixColorDestination.Green,
                blendingState.FixColorDestination.Blue,
                blendingState.FixColorDestination.Alpha
            );
        }

        private void PrepareState_Texture_Common(GpuStateStruct gpuState)
        {
            //Console.WriteLine($"PrepareState_Texture_Common {VertexType}");

            if (VertexType.Transform2D)
            {
                PrepareState_Texture_2D(gpuState);
            }
            else
            {
                PrepareState_Texture_3D(gpuState);
            }

            RenderbufferManager.TextureCacheGetAndBind(gpuState);

            //GL.TexEnvi(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvModeTranslate[(int)TextureState.Effect]);
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

                ShaderInfo.TextureMode.Set((int)TexCoordMode.Default);

                ShaderInfo.matrixTexture.Set(_textureMatrix);
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

                        ShaderInfo.TextureMode.Set((int)TexCoordMode.Default);
                        break;

                    case TextureMapMode.GuTextureMatrix:
                        switch (gpuState.TextureMappingState.TextureProjectionMapMode)
                        {
                            case TextureProjectionMapMode.GuPosition:
                                ShaderInfo.TextureMode.Set((int)TexCoordMode.GU_POSITION);
                                break;
                            case TextureProjectionMapMode.GuNormal:
                                ShaderInfo.TextureMode.Set((int)TexCoordMode.GU_NORMAL);
                                break;
                            case TextureProjectionMapMode.GuNormalizedNormal:
                                ShaderInfo.TextureMode.Set((int)TexCoordMode.GU_NORMALIZED_NORMAL);
                                break;
                            case TextureProjectionMapMode.GuUv:
                                ShaderInfo.TextureMode.Set((int)TexCoordMode.GU_UV);
                                break;
                            default:
                                Console.Error.WriteLine("NotImplemented: GU_TEXTURE_MATRIX: {0}", gpuState.TextureMappingState.TextureProjectionMapMode);
                                break;
                        }
                        //Console.WriteLine($"GuTextureMatrix tfx {textureState.Effect} tcc {textureState.ColorComponent}");
                        break;

                    case TextureMapMode.GuEnvironmentMap:
                        {
                            ShaderInfo.TextureMode.Set((int)TexCoordMode.GU_ENV_MAP);
                        }
                        break;

                    default:
                        Console.Error.WriteLine("NotImplemented TextureMappingState.TextureMapMode: " + textureMappingState.TextureMapMode);
                        break;
                }

                ShaderInfo.matrixTexture.Set(_textureMatrix);
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
                    gpuState.VertexState.WorldMatrix * gpuState.VertexState.ViewMatrix * gpuState.VertexState.ProjectionMatrix;
            }
        }

        public static void PrepareStateClear(GpuStateStruct gpuState)
        {
            bool colorMask = false, alphaMask = false;
            bool depthMask = gpuState.ClearFlags.HasFlag(ClearBufferSet.DepthBuffer);
            bool stencilMask = gpuState.ClearFlags.HasFlag(ClearBufferSet.StencilBuffer);

            GL.Disable(GL.GL_BLEND);
            GL.Disable(GL.GL_LIGHTING);
            GL.Disable(GL.GL_TEXTURE_2D);
            GL.Disable(GL.GL_ALPHA_TEST);
            GL.Disable(GL.GL_DEPTH_TEST);
            GL.Disable(GL.GL_STENCIL_TEST);
            GL.Disable(GL.GL_FOG);
            GL.Disable(GL.GL_LOGIC_OP);
            GL.Disable(GL.GL_CULL_FACE);
            GL.DepthMask(false);

            if (gpuState.ClearFlags.HasFlag(ClearBufferSet.ColorBuffer))
            {
                colorMask = true;
            }

            if (GL.EnableDisable(GL.GL_STENCIL_TEST, stencilMask))
            {
                alphaMask = true;
                GL.StencilFunc(GL.GL_ALWAYS, 0x00, 0xFF);
                GL.StencilOp(GL.GL_REPLACE, GL.GL_REPLACE, GL.GL_REPLACE);
                GL.StencilMask(0xFF);
            }

            if (depthMask)
            {
                GL.Enable(GL.GL_DEPTH_TEST);
                GL.DepthFunc(GL.GL_ALWAYS);
                GL.DepthMask(true);
                GL.DepthRange(0.0f, 0.0f);
                //GL.DepthRange(0.0f, 1.0f); // Original value
            }

            GL.ColorMask(colorMask, colorMask, colorMask, alphaMask);

            GL.ClearDepthf(1.0f);

            uint clearBits = 0;
            if (colorMask) clearBits |= GL.GL_COLOR_BUFFER_BIT;
            if (depthMask) clearBits |= GL.GL_DEPTH_BUFFER_BIT;
            if (stencilMask) clearBits |= GL.GL_STENCIL_BUFFER_BIT;
            if (clearBits != 0)
            {
                GL.Clear(clearBits);
            }
        }

    }

}