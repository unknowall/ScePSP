using ScePSP.Core.GpuBackEnd.State;
using ScePSP.Core.GpuBackEnd.VertexReading;
using ScePSP.Core.Memory;
using ScePSP.Core.Types;
using System;

namespace ScePSP.Core.GpuBackEnd
{
    public abstract unsafe class GpuBackEnd
    {
        public GpuStateStruct GpuState;

        protected PspMemory Memory => PSPDrivers.PspMemory;
        protected PspStoredConfig PspStoredConfig => PSPDrivers.Config.StoredConfig;

        protected int _ScaleViewport = 2;

        internal event Action<int> OnScaleViewport;

        protected VertexInfo[] Vertices = new VertexInfo[ushort.MaxValue];
        protected VertexTypeStruct VertexType;
        protected byte* indexListByte;
        protected ushort* indexListShort;
        protected ReadVertexDelegate readVertex;
        protected VertexReader VertexReader = new VertexReader();

        protected ReadVertexDelegate ReadVertex_Void_delegate;
        protected ReadVertexDelegate ReadVertex_Byte_delegate;
        protected ReadVertexDelegate ReadVertex_Short_delegate;

        public GpuBackEnd()
        {
            ReadVertex_Void_delegate = ReadVertex_Void;
            ReadVertex_Byte_delegate = ReadVertex_Byte;
            ReadVertex_Short_delegate = ReadVertex_Short;
        }

        public int ScaleViewport
        {
            protected set
            {
                OnScaleViewport?.Invoke(value);
                _ScaleViewport = value;
            }
            get => _ScaleViewport;
        }

        public virtual void InitSynchronizedOnce(IntPtr TargetHwnd)
        {
        }

        public virtual void StopSynchronized()
        {
        }

        public virtual void PrimStart(GlobalGpuState globalGpuState, GpuStateStruct gpuState, GuPrimitiveType primitiveType)
        {
        }

        public virtual void PrimEnd()
        {
        }

        public virtual void Prim(ushort vertexCount, bool isPPrim = false)
        {
        }

        public virtual void Finish(GpuStateStruct gpuState)
        {
        }

        public virtual void End(GpuStateStruct gpuState)
        {
        }

        public virtual void Sync(GpuStateStruct lastGpuState)
        {
        }

        public virtual void BeforeDraw(GpuStateStruct gpuState)
        {
        }

        public virtual void InvalidateCache(uint address, int size)
        {
        }

        public virtual void TextureFlush(GpuStateStruct gpuState)
        {
        }

        public virtual void TextureSync(GpuStateStruct gpuState)
        {
        }

        public virtual void AddedGEProcess()
        {
        }

        public virtual void StartCapture()
        {
        }

        public virtual void EndCapture()
        {
        }

        public virtual void Transfer(GpuStateStruct gpuState)
        {
        }

        public virtual void SetCurrent()
        {
        }

        public virtual void UnsetCurrent()
        {
        }

        public virtual void DrawCurvedSurface(GlobalGpuState GlobalGpuState, GpuStateStruct GpuStateStruct, VertexInfo[,] Patch, int UCount, int VCount)
        {
        }

        public virtual void DrawSpline(GlobalGpuState GlobalGpuState, GpuStateStruct GpuStateStruct, VertexInfo[,] Patch,
            int sp_ucount, int sp_vcount, int sp_utype, int sp_vtype, int normalizedUType, int normalizedVType)
        {
        }

        public virtual void DrawVideo(uint FrameBufferAddress, OutputPixel* OutputPixel, int Width, int Height)
        {
        }

        public void ReadVertex_Void(int index, out VertexInfo vertexInfo) => vertexInfo = Vertices[index];

        public void ReadVertex_Byte(int index, out VertexInfo vertexInfo) => vertexInfo = Vertices[indexListByte[index]];

        public void ReadVertex_Short(int index, out VertexInfo vertexInfo) => vertexInfo = Vertices[indexListShort[index]];

        protected delegate void ReadVertexDelegate(int index, out VertexInfo vertexInfo);

        protected void PrepareVertexs(GpuStateStruct GpuState, out uint totalVerticesWithoutMorphing, uint vertexCount, out uint morpingVertexCount)
        {
            totalVerticesWithoutMorphing = vertexCount;

            morpingVertexCount = (uint)(VertexType.MorphingVertexCount + 1);

            VertexType.NormalCount = GpuState.TextureMappingState.GetTextureComponentsCount();

            readVertex = ReadVertex_Void_delegate;

            VertexReader.SetVertexTypeStruct(
                VertexType,
                (byte*)Memory.PspAddressToPointerSafe(GpuState.GetAddressRelativeToBaseOffset(GpuState.VertexAddress), 0)
            );

            void* indexPointer = null;
            if (VertexType.Index != VertexTypeStruct.IndexEnum.Void)
            {
                indexPointer = Memory.PspAddressToPointerSafe(GpuState.GetAddressRelativeToBaseOffset(GpuState.IndexAddress), 0);
            }

            switch (VertexType.Index)
            {
                case VertexTypeStruct.IndexEnum.Void:
                    break;

                case VertexTypeStruct.IndexEnum.Byte:
                    // If index pointer is missing, fallback to non-indexed behaviour:
                    if (indexPointer == null)
                    {
                        // treat as non-indexed: read sequential vertices
                        readVertex = ReadVertex_Void_delegate;
                        indexListByte = null;
                        totalVerticesWithoutMorphing = vertexCount;
                    }
                    else
                    {
                        readVertex = ReadVertex_Byte_delegate;
                        indexListByte = (byte*)indexPointer;
                        totalVerticesWithoutMorphing = 0;
                        if (indexListByte != null)
                        {
                            for (var n = 0; n < vertexCount; n++)
                            {
                                if (totalVerticesWithoutMorphing < indexListByte[n])
                                    totalVerticesWithoutMorphing = indexListByte[n];
                            }
                        }
                    }
                    break;

                case VertexTypeStruct.IndexEnum.Short:
                    // If index pointer is missing, fallback to non-indexed behaviour:
                    if (indexPointer == null)
                    {
                        readVertex = ReadVertex_Void_delegate;
                        indexListShort = null;
                        totalVerticesWithoutMorphing = vertexCount;
                    }
                    else
                    {
                        readVertex = ReadVertex_Short_delegate;
                        indexListShort = (ushort*)indexPointer;
                        totalVerticesWithoutMorphing = 0;
                        if (indexListShort != null)
                        {
                            for (var n = 0; n < vertexCount; n++)
                            {
                                if (totalVerticesWithoutMorphing < indexListShort[n])
                                    totalVerticesWithoutMorphing = indexListShort[n];
                            }
                        }
                    }
                    break;

                default:
                    throw new NotImplementedException("VertexType.Index: " + VertexType.Index);
            }

            // ensure we read at least one vertex when max-index logic yields zero
            if (totalVerticesWithoutMorphing == 0)
            {
                totalVerticesWithoutMorphing = vertexCount;
            }
            totalVerticesWithoutMorphing++;

            // Fix missing geometry! At least!
            if (VertexType.Index == VertexTypeStruct.IndexEnum.Void)
            {
                GpuState.VertexAddress += (uint)(VertexReader.VertexSize * vertexCount * morpingVertexCount);
                //GpuState->VertexAddress += (uint)(VertexReader.VertexSize * VertexCount);
            }

            if (morpingVertexCount != 1 || VertexType.RealSkinningWeightCount != 0)
            {
                //Console.WriteLine("PRIM: {0}, {1}, Morphing:{2}, Skinning:{3}", PrimitiveType, vertexCount, morpingVertexCount, VertexType.RealSkinningWeightCount);
            }
            //Console.WriteLine(TotalVerticesWithoutMorphing);
        }
    }
}