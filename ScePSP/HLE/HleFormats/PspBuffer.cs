using System;
using System.Runtime.InteropServices;

namespace ScePSP.Hle.Formats
{
    public unsafe class PspBuffer : IDisposable
    {
        private byte* buffer;
        private int captSize;
        private int currentSize;
        private int readPosition;
        private int writePosition;
        private bool _disposed;

        private PspBuffer() { }

        public PspBuffer(byte* addr, int CaptSize, int realSize, int Position)
        {
            buffer = (byte*)Marshal.AllocHGlobal(CaptSize);
            long destAvailable = CaptSize;
            long copySize = Math.Min(realSize, destAvailable);
            Buffer.MemoryCopy(addr, buffer, destAvailable, copySize);

            currentSize = realSize;
            readPosition = Position;
            writePosition = realSize;
            captSize = CaptSize;
        }

        public void Dispose()
        {
            if (!_disposed && buffer != null)
            {
                Marshal.FreeHGlobal((IntPtr)buffer);
                buffer = null;
                _disposed = true;
            }
            //GC.SuppressFinalize(this);
        }

        ~PspBuffer() => Dispose();

        public int Available() => currentSize - readPosition;

        public int AvailableWriteSize() => captSize - writePosition;

        public void Write(byte* addr, int Size)
        {
            if (addr == null || Size <= 0 || _disposed) return;

            long destAvailable = AvailableWriteSize();
            long copySize = Math.Min(Size, destAvailable);
            Buffer.MemoryCopy(addr, buffer + writePosition, destAvailable, copySize);

            notifyWrite((int)copySize);
        }

        public int Position
        {
            get => readPosition;
            set
            { 
                readPosition = Math.Clamp(value, 0, currentSize); 
            }
        }

        public bool BufEnd => readPosition >= captSize;

        public byte* ReadAddr => buffer + readPosition;

        public byte* WriteAddr => buffer + writePosition;

        public int CurrentSize => currentSize;

        public int MaxSize => captSize;

        public void notifyRead(int size)
        {
            if (size > 0)
            {
                size = Math.Min(size, currentSize);
                readPosition = incrementPosition(readPosition, size);
            }
        }

        public void notifyReadAll() => notifyRead(currentSize);

        public void notifyWrite(int size)
        {
            if (size > 0)
            {
                size = Math.Min(size, captSize - currentSize);
                writePosition = incrementPosition(writePosition, size);
                currentSize += size;
            }
        }

        private int incrementPosition(int position, int size)
        {
            position += size;
            return position >= captSize ? captSize : position;
        }

        public virtual bool Empty => currentSize == 0;
    }
}