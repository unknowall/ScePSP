using ScePSP.Core.GpuBackEnd.State;
using ScePSP.Utils;
using ScePSPUtils.Drawing;
using System;
using System.Numerics;

namespace ScePSP.Core.GpuBackEnd.Soft
{
    unsafe public class SoftBackEnd : GpuBackEnd
    {
        private TriangleRasterizer<Triangle> TriangleRasterizer;

        public SoftBackEnd()
        {
            TriangleRasterizer = new TriangleRasterizer<Triangle>((int y, ref RasterizerResult a, ref RasterizerResult b, ref Triangle triangle) =>
            {
                //Console.WriteLine($"{a.Ratio} {b.Ratio} {triangle.P0.Color} : {triangle.P1.IColor}");
                var P0 = triangle.P0;
                var P1 = triangle.P1;
                var P2 = triangle.P2;
                var C0 = P0.Color;
                var C1 = P1.Color;
                var C2 = P2.Color;
                //Console.WriteLine(triangle.P0.T);
                if (VertexType.HasTexture)
                {
                    var texA = LerpRatios(P0.T, P1.T, P2.T, a.Ratios);
                    var texB = LerpRatios(P0.T, P1.T, P2.T, b.Ratios);
                    //Console.WriteLine($"{P0.T} {P1.T} {P2.T} {a.Ratios} {b.Ratios} {texA} {texB}");
                    DrawRowFastTextureLookup(y, a.X, b.X, texA, texB);
                }
                else if (C0 == C1 && C0 == C2)
                {
                    DrawRowFastSingleColor(y, a.X, b.X, triangle.P0.RgbaColor);
                }
                else
                {
                    var colorA = LerpRatios(C0, C1, C2, a.Ratios).Clamp(0, 1);
                    var colorB = LerpRatios(C0, C1, C2, b.Ratios).Clamp(0, 1);
                    //Console.WriteLine($"{a.Ratio0}, {a.Ratio1}, {a.Ratio2}");
                    DrawRowFastInterpolateTwoColors(y, a.X, b.X, colorA, colorB);
                }
            }, 0, 271, 0, 511);
        }

        static public Vector4 LerpRatios(Vector4 a, Vector4 b, Vector4 c, float ra, float rb, float rc) => new Vector4(
            a.X * ra + b.X * rb + c.X * rc,
            a.Y * ra + b.Y * rb + c.Y * rc,
            a.Z * ra + b.Z * rb + c.Z * rc,
            a.W * ra + b.W * rb + c.W * rc
        );

        static public Vector4 LerpRatios(Vector4 a, Vector4 b, Vector4 c, Vector3 ratios) => LerpRatios(a, b, c, ratios.X, ratios.Y, ratios.Z);

        private VPoint Vector4ToPoint(Vector4 v, Vector4 n, Vector4 t, Vector4 color) => new VPoint(new RasterizerPoint((int)v.X, (int)v.Y), n, t, color);

        private uint[] colors = { 0xFF0077FF, 0xFF00FFFF, 0xFF0000FF };

        public struct VPoint
        {
            public RasterizerPoint P;
            public Vector4 N;
            public Vector4 T;
            public Vector4 Color;

            public Rgba RgbaColor => new RgbaFloat(Color).Rgba;
            public uint IColor => new RgbaFloat(Color).Int;

            public VPoint(RasterizerPoint p, Vector4 n, Vector4 t, Vector4 color)
            {
                P = p;
                N = n;
                T = t;
                Color = color;
            }

            public override string ToString() => $"VPoint({P}, {N}, {T}, {Color})";
        }

        public struct Triangle
        {
            public VPoint P0;
            public VPoint P1;
            public VPoint P2;

            public Triangle(VPoint p0, VPoint p1, VPoint p2)
            {
                // Sort the points so that y0 <= y1 <= y2
                if (p1.P.Y < p0.P.Y) Swap(ref p1, ref p0);
                if (p2.P.Y < p0.P.Y) Swap(ref p2, ref p0);
                if (p2.P.Y < p1.P.Y) Swap(ref p2, ref p1);


                P0 = p0;
                P1 = p1;
                P2 = p2;
            }
        }

        public struct Line
        {
            public VPoint P0;
            public VPoint P1;

            public Line(VPoint p0, VPoint p1)
            {
                // Sort the points so that y0 <= y1 <= y2
                if (p1.P.Y < p0.P.Y) Swap(ref p1, ref p0);
                P0 = p0;
                P1 = p1;
            }
        }

        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            var temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        protected Matrix4x4 modelView = Matrix4x4.Identity;
        protected Matrix4x4 worldViewProjection3D = Matrix4x4.Identity;
        protected Matrix4x4 worldViewProjection2D = Matrix4x4.Identity;
        protected Matrix4x4 transform3d = Matrix4x4.Identity;

        protected Matrix4x4 unitToScreenCoords = Matrix4x4.CreateScale(1f, -1f, 1f) *
                                               Matrix4x4.CreateTranslation(+1f, +1f, 0f) *
                                               Matrix4x4.CreateScale(.5f, .5f, 1f) *
                                               Matrix4x4.CreateScale(480, 272, 1f);

        protected uint DrawAddress;
        protected GuPrimitiveType PrimitiveType;

        public override void PrimStart(GlobalGpuState globalGpuState, GpuStateStruct gpuState, GuPrimitiveType primitiveType)
        {
            GpuState = gpuState;
            VertexType = gpuState.VertexState.Type;
            PrimitiveType = primitiveType;
            var model = gpuState.VertexState.WorldMatrix;
            var view = gpuState.VertexState.ViewMatrix;
            var projection3D = gpuState.VertexState.ProjectionMatrix;
            var projection2D = Matrix4x4.CreateOrthographic(480, 272, -1f, +1f);
            //Matrix4x4.Invert(projection2D, out unitToScreenCoords);
            modelView = model * view;
            worldViewProjection3D = modelView * projection3D;
            worldViewProjection2D = modelView * projection2D;
            DrawAddress = GpuState.DrawBufferState.Address;
            //if (primitiveType == GuPrimitiveType.Triangles)
            //{
            //    Console.WriteLine($"primitiveType {primitiveType}");
            //    Console.WriteLine($"Model {model}");
            //    Console.WriteLine($"View {view}");
            //    Console.WriteLine($"Projection3D {projection3D}");
            //    Console.WriteLine($"Projection2D {projection2D}");
            //    Console.WriteLine($"worldView {modelView}");
            //    Console.WriteLine($"worldViewProjection3D {worldViewProjection3D}");
            //    Console.WriteLine($"worldViewProjection2D {worldViewProjection2D}");
            //    Console.WriteLine($"PrimStart: {primitiveType}");
            //}
        }

        public override void Prim(ushort vertexCount)
        {
            uint morpingVertexCount, totalVerticesWithoutMorphing;
            PrepareVertexs(GpuState, out totalVerticesWithoutMorphing, vertexCount, out morpingVertexCount);
            var vertices = stackalloc Vector4[vertexCount];
            var vP = stackalloc VPoint[vertexCount];
            var vertexTransform = this.worldViewProjection3D;
            {
                var transform = VertexType.Transform2D ? Matrix4x4.Identity : worldViewProjection3D;
                var rtransform = VertexType.Transform2D ? transform : transform * unitToScreenCoords;

                for (var n = 0; n < vertexCount; n++)
                {
                    var vinfo = VertexReader.ReadVertex(n);
                    var vector = vinfo.Position.ToVector4();
                    var tvertex = VertexType.Transform2D ? vector : Vector4.Transform(vector, rtransform);

                    //Console.WriteLine($"VertexType.Transform2D: {VertexType.Transform2D} : {PrimitiveType} : {vinfo}");

                    vertices[n] = tvertex;
                    var color = VertexType.HasColor ? vinfo.Color : GpuState.LightingState.AmbientModelColor.ToVector4();

                    //Console.WriteLine($"AmbientLightColor: ", GpuState.LightingState.AmbientLightColor);

                    vP[n] = Vector4ToPoint(tvertex, vinfo.Normal, vinfo.Texture, color);
                }
            }
            {
                switch (PrimitiveType)
                {
                    case GuPrimitiveType.Points:
                        {
                            for (var n = 0; n < vertexCount; n++) DrawPoint(vP[n]);
                            break;
                        }
                    case GuPrimitiveType.Lines:
                        {
                            for (var n = 0; n < vertexCount; n += 2) DrawLine(vP[n], vP[n + 1]);
                            break;
                        }
                    case GuPrimitiveType.LineStrip:
                        {
                            for (var n = 1; n < vertexCount; n++) DrawLine(vP[n - 1], vP[n]);
                            break;
                        }
                    case GuPrimitiveType.Triangles:
                        {
                            for (var n = 0; n < vertexCount; n += 3) DrawTriangle(vP[n + 0], vP[n + 1], vP[n + 2]);
                            break;
                        }
                    case GuPrimitiveType.TriangleStrip:
                        {
                            for (var n = 2; n < vertexCount; n++) DrawTriangle(vP[n - 2], vP[n - 1], vP[n]);
                            break;
                        }
                    case GuPrimitiveType.TriangleFan:
                        {
                            for (var n = 2; n < vertexCount; n++) DrawTriangle(vP[0], vP[n - 1], vP[n]);
                            break;
                        }
                    case GuPrimitiveType.Sprites:
                        {
                            for (var n = 0; n < vertexCount; n += 2) DrawSprite(vP[n + 0], vP[n + 1]);
                            break;
                        }
                    default:
                        Console.WriteLine($"Unsupported {PrimitiveType}");
                        break;
                }
            }
        }

        protected void DrawPoint(VPoint a)
        {
            VPoint b = new VPoint(new RasterizerPoint(a.P.X, a.P.Y + 1), a.N, a.T, a.Color);
            DrawLine(a, b);
        }

        protected void DrawSprite(VPoint a, VPoint b)
        {
            VPoint ar = new VPoint(new RasterizerPoint(b.P.X, a.P.Y), a.N, a.T, a.Color);
            VPoint bl = new VPoint(new RasterizerPoint(a.P.X, b.P.Y), b.N, b.T, b.Color);
            DrawTriangle(a, ar, bl);
            DrawTriangle(bl, ar, b);
        }

        protected void DrawTriangle(VPoint a, VPoint b, VPoint c)
        {
            var triangle = new Triangle(a, b, c);
            TriangleRasterizer.RasterizeTriangle(triangle.P0.P, triangle.P1.P, triangle.P2.P, triangle);
        }

        protected void DrawLine(VPoint a, VPoint b)
        {
            var triangle = new Triangle(a, b, b);
            //Console.WriteLine($"DrawLine({a}, {b})");
            TriangleRasterizer.RasterizeLine(a.P, b.P, triangle);
        }

        //protected override void DrawSprite(VPoint a, VPoint b)
        //{
        //    var minX = Math.Min(a.P.X, b.P.X).Clamp(0, 511);
        //    var maxX = Math.Max(a.P.X, b.P.X).Clamp(0, 511);
        //    var minY = Math.Min(a.P.Y, b.P.Y).Clamp(0, 271);
        //    var maxY = Math.Max(a.P.Y, b.P.Y).Clamp(0, 271);
        //    //Console.WriteLine($"Sprite {a}, {b}");
        //    for (var y = minY; y <= maxY; y++) DrawPixelsFast(y, minX, maxX, a.IColor);
        //}

        private uint* ScreenRow(int y)
        {
            return (uint*)Memory.PspAddressToPointerSafe((uint)(DrawAddress + y * 512 * 4));
        }

        private void DrawRowFastSingleColor(int y, int x0, int x1, Rgba color)
        {
            var ptr = (Rgba*)ScreenRow(y);
            for (var n = x0; n < x1; n++) ptr[n] = color;
        }

        private void DrawRowFastInterpolateTwoColors(int y, int x0, int x1, Vector4 c1, Vector4 c2)
        {
            var ptr = (uint*)ScreenRow(y);
            for (var n = x0; n < x1; n++) ptr[n] = new RgbaFloat(Vector4.Lerp(c1, c2, n.RatioInRange(x0, x1))).Int;
        }

        private void DrawRowFastTextureLookup(int y, int x0, int x1, Vector4 uv1, Vector4 uv2)
        {
            var ptr = (uint*)ScreenRow(y);
            for (var n = x0; n < x1; n++) ptr[n] = TextureLookup(Vector4.Lerp(uv1, uv2, n.RatioInRange(x0, x1)));
        }

        public uint TextureLookup(Vector4 pos) => TextureLookup((int)pos.X, (int)pos.Y);

        public uint TextureLookup(int x, int y) => *TextureLookupAddress(x, y);

        public uint* TextureLookupAddress(Vector4 pos) => TextureLookupAddress((int)pos.X, (int)pos.Y);

        public uint* TextureLookupAddress(int x, int y)
        {
            var textureMappingStateStruct = GpuState.TextureMappingState;
            var textureStateStruct = textureMappingStateStruct.TextureState;
            var mipmap = textureStateStruct.Mipmap0;
            return (uint*)Memory.PspAddressToPointerSafe((uint)(mipmap.Address + (y * mipmap.BufferWidth + x) * 4));
        }

    }
}