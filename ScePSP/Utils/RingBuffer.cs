using System;

namespace ScePSPUtils
{
    public unsafe class ByteRingBufferWrapper
    {
        private byte* _dataPointer;
        private int _dataLength;
        private long _readPosition;
        private long _writePosition;

        private ByteRingBufferWrapper()
        {
            _readPosition = 0;
        }

        public static ByteRingBufferWrapper FromPointer(byte* pointer, int dataLength)
        {
            return new ByteRingBufferWrapper
            {
                _dataPointer = pointer,
                _dataLength = dataLength,
            };
        }

        public int Capacity => _dataLength;

        public long ReadAvailable => _writePosition - _readPosition;

        public long WriteAvailable => Capacity - ReadAvailable;

        public void Write(byte item)
        {
            if (WriteAvailable <= 0) throw new OverflowException("RingBuffer is full");
            _dataPointer[_writePosition++ % Capacity] = item;
        }

        public byte Read()
        {
            if (ReadAvailable <= 0) throw new OverflowException("RingBuffer is empty");
            return _dataPointer[_readPosition++ % Capacity];
        }

        public int Write(byte[] transferData, int offset = 0, int length = -1)
        {
            if (length == -1) length = _dataLength - offset;
            length = Math.Min(length, (int)WriteAvailable);
            var transferred = 0;
            while (length-- > 0)
            {
                Write(transferData[offset++]);
                transferred++;
            }
            return transferred;
        }

        public int Read(byte[] transferData, int offset = 0, int length = -1)
        {
            if (length == -1) length = _dataLength - offset;
            length = Math.Min(length, (int)ReadAvailable);
            var transferred = 0;
            while (length-- > 0)
            {
                transferData[offset++] = Read();
                transferred++;
            }
            return transferred;
        }
    }

    public class RingBuffer<T>
    {
        private T[] Data;
        private long _readPosition;
        private long _writePosition;

        public RingBuffer(int capacity)
        {
            Data = new T[capacity];
        }

        public int Capacity => Data.Length;

        public long ReadAvailable => _writePosition - _readPosition;

        public long WriteAvailable => Capacity - ReadAvailable;

        public void Write(T item)
        {
            if (WriteAvailable <= 0) throw new OverflowException("RingBuffer is full");
            Data[_writePosition++ % Capacity] = item;
        }

        public T Read()
        {
            if (ReadAvailable <= 0) throw new OverflowException("RingBuffer is empty");
            return Data[_readPosition++ % Capacity];
        }

        public int Write(T[] transferData, int offset = 0, int length = -1)
        {
            if (length == -1) length = Data.Length - offset;
            length = Math.Min(length, (int)WriteAvailable);
            var transferred = 0;
            while (length-- > 0)
            {
                Write(transferData[offset++]);
                transferred++;
            }
            return transferred;
        }

        public int Read(T[] transferData, int offset = 0, int length = -1)
        {
            if (length == -1) length = Data.Length - offset;
            length = Math.Min(length, (int)ReadAvailable);
            var transferred = 0;
            while (length-- > 0)
            {
                transferData[offset++] = Read();
                transferred++;
            }
            return transferred;
        }
    }
}