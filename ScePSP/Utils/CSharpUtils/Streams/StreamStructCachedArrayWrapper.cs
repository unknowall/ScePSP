using ScePSPUtils.Arrays;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ScePSPUtils.Streams
{
    public class StreamStructCachedArrayWrapper<TType> : IArray<TType> where TType : struct
    {
        readonly List<TType> _cachedValues = new List<TType>();
        readonly int _numberOfItemsToBuffer;
        readonly Stream _stream;
        static readonly Type StructType = typeof(TType);
        private static readonly int StructSize = Marshal.SizeOf(StructType);

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private Task _currentReadTask = Task.CompletedTask;

        private int BufferedItemsCount => _cachedValues.Count;

        public int Length => (int)(_stream.Length / StructSize);

        public StreamStructCachedArrayWrapper(int numberOfItemsToBuffer, Stream stream)
        {
            _numberOfItemsToBuffer = numberOfItemsToBuffer;
            _stream = stream;
        }

        private void SecureUpToItem(int maxItem)
        {
            maxItem = Math.Min(maxItem, Length);
            var itemsToRead = maxItem - BufferedItemsCount;

            if (itemsToRead > 0)
            {
                try
                {
                    var dataLength = itemsToRead * StructSize;
                    var data = new byte[dataLength];

                    _currentReadTask = _currentReadTask.ContinueWith(async (prev) =>
                    {
                        await _semaphore.WaitAsync();
                        try
                        {
                            if (_cachedValues.Count >= maxItem) return;
                            var readed = _stream.Read(data, 0, dataLength);
                            if (readed > 0)
                            {
                                _cachedValues.AddRange(PointerUtils.ByteArrayToArray<TType>(data));
                            }
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }).Unwrap();
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception);
                }
            }
        }

        private void SecureUpToItem(int offset, int numberOfItemsToBuffer)
        {
            if (BufferedItemsCount - offset < numberOfItemsToBuffer / 2)
            {
                while (true)
                {
                    if (BufferedItemsCount - offset < numberOfItemsToBuffer / 2)
                    {
                        SecureUpToItem(BufferedItemsCount + numberOfItemsToBuffer);
                    }

                    if (offset >= BufferedItemsCount)
                    {
                        _currentReadTask.Wait();
                        Thread.Sleep(1);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public IEnumerator<TType> GetEnumerator()
        {
            for (var n = 0; n < Length; n++) yield return this[n];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            for (var n = 0; n < Length; n++) yield return this[n];
        }

        public TType this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Invalid Index {index}. Must be in range 0-{Length}");
                SecureUpToItem(index, _numberOfItemsToBuffer);
                _currentReadTask.Wait();
                return _cachedValues[index];
            }
            set => throw new NotImplementedException();
        }

        public TType[] GetArray()
        {
            SecureUpToItem(Length);
            _currentReadTask.Wait();
            return _cachedValues.ToArray();
        }

    }
}