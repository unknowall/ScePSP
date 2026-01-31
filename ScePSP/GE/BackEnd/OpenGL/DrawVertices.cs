using LightGL;
using ScePSP.GE;
using ScePSP.GE.State;
using ScePSPUtils.Drawing;
using System;
using System.Numerics;

namespace ScePSP.BackEnd.OpenGL
{
    public unsafe partial class GLBackEnd : GEBackEnd
    {
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

        private void DrawInitVertices()
        {
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

            PutVertexIndex(_verticesPosition.Length);

            _verticesPosition.Add(vertexInfo.Position.ToVector3());
            _verticesNormal.Add(vertexInfo.Normal.ToVector3());
            _verticesTexcoords.Add(vertexInfo.Texture.ToVector3());
            _verticesColors.Add(new RgbaFloat(vertexInfo.Color));
            _verticesWeights.Add(new VertexInfoWeights(vertexInfo));
        }

        private void EndVertex()
        {
            //Console.Out.WriteLineColored(ConsoleColor.Green, $"DrawVertices Geometr: {_primitiveType.ToString()}");
            if (_indicesList.Length > 0)
                DrawVertices(ConvertGLGeometry(_primitiveType));

            ResetVertex();
        }

    }

}