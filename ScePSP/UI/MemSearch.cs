using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScePSP.UI
{
    public unsafe class MemorySearch
    {
        private byte* data;
        private int dataLength;
        public List<int> results;

        public MemorySearch(byte* memory, int length)
        {
            data = memory;
            dataLength = length;
            ResetResults();
        }

        public void UpdateData(byte* newMemory, int newLength)
        {
            data = newMemory;
            dataLength = newLength;
        }

        public void ResetResults()
        {
            results = Enumerable.Range(0, dataLength).ToList();
        }

        public void SearchByte(byte value)
        {
            results = Search((index) => data[index] == value);
        }

        public void SearchWord(ushort value)
        {
            results = Search((index) =>
                index + 1 < dataLength && *(ushort*)(data + index) == value);
        }

        public void SearchDword(uint value)
        {
            results = Search((index) =>
                index + 3 < dataLength && *(uint*)(data + index) == value);
        }

        public void SearchFloat(float value)
        {
            results = Search((index) =>
                index + 3 < dataLength && *(float*)(data + index) == value);
        }

        public void SearchPointer(uint targetAddress)
        {
            results = Search((index) =>
                index + 3 < dataLength && *(uint*)(data + index) == targetAddress);
        }

        public void SearchPointer16(ushort targetAddress)
        {
            results = Search((index) =>
                index + 1 < dataLength && *(ushort*)(data + index) == targetAddress);
        }

        public List<(int Address, object Value)> GetResults()
        {
            var resultValues = new List<(int, object)>();
            foreach (var index in results)
            {
                if (index >= dataLength) continue;

                object value = null;
                if (index + 3 < dataLength)
                {
                    value = $"0x{*(uint*)(data + index):X8}";
                }
                else if (index + 1 < dataLength)
                {
                    value = $"0x{*(ushort*)(data + index):X4}";
                }
                else
                {
                    value = $"0x{data[index]:X2}";
                }
                resultValues.Add((index, value));
            }
            return resultValues;
        }

        private List<int> Search(Func<int, bool> condition)
        {
            var newResults = new List<int>();

            Parallel.ForEach(Partitioner.Create(0, results.Count), range =>
            {
                var localResults = new List<int>();
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    int index = results[i];
                    if (condition(index))
                        localResults.Add(index);
                }

                lock (newResults)
                {
                    newResults.AddRange(localResults);
                }
            });

            return newResults;
        }
    }
}