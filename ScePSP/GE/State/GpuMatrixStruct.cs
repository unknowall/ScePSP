using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ScePSP.GE.State
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct GpuMatrix4x4Struct
    {
        public readonly static int[] Indexes = new int[] {
            0, 1, 2, 3,
            4, 5, 6, 7,
            8, 9, 10, 11,
            12, 13, 14, 15
        };

        public fixed float Values[4 * 4];

        public uint Index;

        internal void Reset(uint Index = 0)
        {
            this.Index = Index;
        }

        public void Dump()
        {
            fixed (float* ValuesPtr = Values)
            {
                Console.WriteLine("----------------------");
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        Console.Write("{0}, ", ValuesPtr[y * 4 + x]);
                    }
                    Console.WriteLine("");
                }
                Console.WriteLine("----------------------");
            }
        }

        internal void Write(float Value)
        {
            //if (Index < Indexes.Length)
            {
                fixed (float* ValuesPtr = Values)
                {
                    ValuesPtr[Indexes[Index++ % Indexes.Length]] = Value;
                }
            }
        }

        public void SetIdentity()
        {
            fixed (float* ValuesPtr = Values)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        ValuesPtr[x + y * 4] = (x == y) ? 1 : 0;
                    }
                }
            }
        }

        public Matrix4x4 Matrix4
        {
            get
            {
                fixed (float* ValuesPtr = Values)
                {
                    var Matrix4 = new Matrix4x4(
                        ValuesPtr[0], ValuesPtr[1], ValuesPtr[2], ValuesPtr[3],
                        ValuesPtr[4], ValuesPtr[5], ValuesPtr[6], ValuesPtr[7],
                        ValuesPtr[8], ValuesPtr[9], ValuesPtr[10], ValuesPtr[11],
                        ValuesPtr[12], ValuesPtr[13], ValuesPtr[14], ValuesPtr[15]
                    );
                    //Matrix4x4.Transpose();
                    return Matrix4;
                }
            }
        }

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct GpuMatrix4x3Struct
    {
        public readonly static int[] Indexes = new int[] {
            0, 1, 2,
            4, 5, 6,
            8, 9, 10,
            12, 13, 14
        };

        public fixed float Values[4 * 4];

        public uint Index;

        internal void Reset(uint Index = 0)
        {
            fixed (float* ValuesPtr = Values)
            {
                ValuesPtr[15] = 1.0f;
            }
            this.Index = Index;
        }

        public void Dump()
        {
            fixed (float* ValuesPtr = Values)
            {
                Console.WriteLine("----------------------");
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        Console.Write("{0}, ", ValuesPtr[y * 4 + x]);
                    }
                    Console.WriteLine("");
                }
                Console.WriteLine("----------------------");
            }
        }

        internal void Write(float Value)
        {
            //if (Index < Indexes.Length)
            {
                fixed (float* ValuesPtr = Values)
                {
                    ValuesPtr[Indexes[Index++ % Indexes.Length]] = Value;
                }
            }
        }

        internal void WriteAt(int Index, float Value)
        {
            //if (Index < Indexes.Length)
            {
                fixed (float* ValuesPtr = Values)
                {
                    ValuesPtr[Indexes[Index]] = Value;
                }
            }
        }

        public Matrix4x4 Matrix4
        {
            get
            {
                fixed (float* ValuesPtr = Values)
                {
                    var Matrix4 = new Matrix4x4(
                        ValuesPtr[0], ValuesPtr[1], ValuesPtr[2], 0,
                        ValuesPtr[4], ValuesPtr[5], ValuesPtr[6], 0,
                        ValuesPtr[8], ValuesPtr[9], ValuesPtr[10], 0,
                        ValuesPtr[12], ValuesPtr[13], ValuesPtr[14], 1.0f
                    );
                    //Matrix4x4.Transpose();
                    return Matrix4;
                }
            }
        }

        public void SetPosition(int Column, int Row, float Value)
        {
            fixed (float* ValuesPtr = Values)
            {
                ValuesPtr[Row * 4 + Column] = Value;
            }
        }

        public void LoadIdentity()
        {
            for (int Row = 0; Row < 4; Row++)
            {
                for (int Column = 0; Column < 4; Column++)
                {
                    SetPosition(Column, Row, (Column == Row) ? 1f : 0f);
                }
            }
        }
    }
}
