using LightGL;
using ScePSP.Core.GpuBackEnd.State;
using ScePSP.Utils;
using System;
using System.Numerics;

namespace ScePSP.Core.GpuBackEnd.OpenGL
{
    public unsafe partial class OpenglBackEnd : GpuBackEnd
    {

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

        private void PrepareDrawStateFirst()
        {
            if (_shader == null) DrawInitVertices();

            var vertexType = GpuState.VertexState.Type;

            ShaderInfo.matrixWorldViewProjection.Set(_worldViewProjectionMatrix);
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
                //GL.Disable(EnableCap.Lighting);
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

            //GL.EnableDisable(EnableCap.CullFace, false);

            GL.CullFace(gpuState.BackfaceCullingState.FrontFaceDirection == FrontFaceDirectionEnum.ClockWise
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
            //if (ShaderInfo.uniformColor != null)
            //    ShaderInfo.uniformColor.Set(gpuState.LightingState.AmbientModelColor.ToVector4());

            GL.EnableDisable(GL.GL_COLOR_MATERIAL, VertexType.HasColor);
        }

        private void PrepareState_Lighting(GpuStateStruct gpuState)
        {
            var lighting = gpuState.LightingState;

            if (!lighting.Enabled)
            {
                GL.Disable(GL.GL_LIGHTING);
                // 如果光照被禁用，使用环境色作为基础颜色
                ShaderInfo.uniformColor.NoWarning().Set(lighting.AmbientModelColor.ToVector4());
                return;
            }

            // 合成一个简单的基色 Ambient + Emissive + Diffuse（相加并 clamp 到 [0,1]）
            //var ambient = lighting.AmbientModelColor.ToVector4();
            //var emissive = lighting.EmissiveModelColor.ToVector4();
            //var diffuse = lighting.DiffuseModelColor.ToVector4();

            //var combined = new Vector4(
            //    MathF.Min(1f, ambient.X + emissive.X + diffuse.X),
            //    MathF.Min(1f, ambient.Y + emissive.Y + diffuse.Y),
            //    MathF.Min(1f, ambient.Z + emissive.Z + diffuse.Z),
            //    1f
            //);

            //ShaderInfo.uniformColor.NoWarning().Set(combined);

            //Console.WriteLine($"PrepareState_Lighting {lighting.LightModel}");

            GL.Enable(GL.GL_LIGHTING);

            Vector4 color = gpuState.LightingState.EmissiveModelColor.ToVector4();
            GL.Materialfv(GL.GL_FRONT, GL.GL_EMISSION, (float*)&color);

            color = gpuState.LightingState.AmbientModelColor.ToVector4();
            GL.Materialfv(GL.GL_FRONT, GL.GL_AMBIENT, (float*)&color);

            color = gpuState.LightingState.DiffuseModelColor.ToVector4();
            GL.Materialfv(GL.GL_FRONT, GL.GL_DIFFUSE, (float*)&color);

            color = gpuState.LightingState.SpecularModelColor.ToVector4();
            GL.Materialfv(GL.GL_FRONT, GL.GL_SPECULAR, (float*)&color);

            color = gpuState.LightingState.EmissiveModelColor.ToVector4();
            GL.Materialf(GL.GL_FRONT, GL.GL_SHININESS, gpuState.LightingState.SpecularPower);

            GL.LightModeli(
                (uint)LightModelParameter.LightModelColorControl,
                (uint)((lighting.LightModel == LightModelEnum.SeparateSpecularColor) ? LightModelColorControl.SeparateSpecularColor : LightModelColorControl.SingleColor)
            );

            color = lighting.AmbientLightColor.ToVector4();

            GL.LightModelfv((uint)LightModelParameter.LightModelAmbient, (float*)&color);

            for (byte n = 0; n < 4; n++)
            {
                var LightState = lighting.Light(n);

                uint LightName = GL.GL_LIGHT0 + (uint)n;

                //Console.WriteLine($"LightState {n} {LightState.Enabled}");

                if (LightState.Enabled)
                {
                    GL.Enable((int)LightName);
                }
                else
                {
                    GL.Disable((int)LightName);

                    continue;
                }

                color = LightState.SpecularColor.ToVector4();
                GL.Lightfv(LightName, (uint)LightParameter.Specular, (float*)&color);

                color = LightState.AmbientColor.ToVector4();
                GL.Lightfv(LightName, (uint)LightParameter.Ambient, (float*)&color);

                color = LightState.DiffuseColor.ToVector4();
                GL.Lightfv(LightName, (uint)LightParameter.Diffuse, (float*)&color);

                Vector4 Position = LightState.Position.ToVector4();
                Position.W = 1.0f;
                GL.Lightfv(LightName, (uint)LightParameter.Position, (float*)&Position);

                GL.Lightf(LightName, (uint)LightParameter.ConstantAttenuation, LightState.Attenuation.Constant);
                GL.Lightf(LightName, (uint)LightParameter.LinearAttenuation, LightState.Attenuation.Linear);
                GL.Lightf(LightName, (uint)LightParameter.QuadraticAttenuation, LightState.Attenuation.Quadratic);

                if (LightState.Type == LightTypeEnum.SpotLight)
                {
                    Position = LightState.SpotDirection.ToVector4();
                    Position.W = 0.0f;
                    GL.Lightfv(LightName, (uint)LightParameter.SpotDirection, (float*)&Position);

                    GL.Lightf(LightName, (uint)LightParameter.SpotExponent, LightState.SpotExponent);
                    GL.Lightf(LightName, (uint)LightParameter.SpotCutoff, LightState.SpotCutoff);
                }
                else
                {
                    GL.Lightf(LightName, (uint)LightParameter.SpotExponent, 0f);
                    GL.Lightf(LightName, (uint)LightParameter.SpotCutoff, 180f);
                }
            }
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

                //GL.ActiveTexture(TextureUnit.Texture0);
                //GL.MatrixMode(MatrixMode.Texture);
                //GL.LoadIdentity();
                //
                //GL.Scale(
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
            //var textureMappingState = gpuState.TextureMappingState;
            //var ClutState = TextureMappingState.ClutState;
            //var textureState = textureMappingState.TextureState;

            //if (!GL.EnableDisable(GL.GL_TEXTURE_2D, textureMappingState.Enabled)) return;

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

            //GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvModeTranslate[(int)TextureState.Effect]);
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

            GL.Viewport(left, top, width, height);

            //GL.Disable(GL.GL_LIGHTING);
            GL.Disable(GL.GL_POLYGON_OFFSET_FILL);

            GL.EnableDisable(GL.GL_DITHER, true);

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