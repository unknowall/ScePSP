using ScePSP.Core.Memory;
using System;

namespace ScePSP.Hle.Managers
{
    public enum MemoryPartitions : int
    {
        Kernel0 = 0,
        Kernel1 = 1,
        User = 2,
        VolatilePartition = 3,
        UserStacks = 6,
    }

    public class HleMemoryManager : IInjectInitialize
    {
        public enum BlockTypeEnum : int
        {
            Low = 0,
            High = 1,
            Address = 2,
            LowAligned = 3,
            HighAligned = 4,
        }

        //public MemoryPartition RootPartition = new MemoryPartition(PspMemory.MainOffset, PspMemory.MainOffset + PspMemory.MainSize);
        [Inject] public PspMemory Memory;

        [Inject] InjectContext InjectContext;

        public HleUidPool<MemoryPartition> MemoryPartitionsUid = new HleUidPool<MemoryPartition>();

        public MemoryPartition GetPartition(MemoryPartitions Partition)
        {
            return MemoryPartitionsUid.Get((int)Partition);
        }

        private HleMemoryManager()
        {
        }

        void IInjectInitialize.Initialize()
        {
            MemoryPartition mp;

            mp = MemoryPartitionsUid.Set((int)MemoryPartitions.Kernel0,
                new MemoryPartition(InjectContext, Low: 0x88000000, High: 0x88300000, Allocated: false,
                Name: "Kernel Partition 1")); // 3MB

            Console.Out.WriteLineColored(ConsoleColor.White, $"  -> {mp.ToString()}");

            mp = MemoryPartitionsUid.Set((int)MemoryPartitions.Kernel1,
                new MemoryPartition(InjectContext, Low: 0x88300000, High: 0x88400000, Allocated: false,
                Name: "Kernel Partition 2")); // 1MB

            Console.Out.WriteLineColored(ConsoleColor.White, $"  -> {mp.ToString()}");

            mp = MemoryPartitionsUid.Set((int)MemoryPartitions.User,
                new MemoryPartition(InjectContext, Low: 0x08800000, High: PspMemory.MainSegment.High, Allocated: false,
                    Name: "User Partition")); // 24MB

            Console.Out.WriteLineColored(ConsoleColor.White, $"  -> {mp.ToString()}");

            mp = MemoryPartitionsUid.Set((int)MemoryPartitions.UserStacks,
                new MemoryPartition(InjectContext, Low: 0x08800000, High: 0x0B000000, Allocated: false,
                    Name: "User Stacks Partition")); // 24MB

            Console.Out.WriteLineColored(ConsoleColor.White, $"  -> {mp.ToString()}");

            mp = MemoryPartitionsUid.Set(5,
                new MemoryPartition(InjectContext, Low: 0x08400000, High: 0x08800000, Allocated: false,
                    Name: "Volatile Partition")); // 4MB

            Console.Out.WriteLineColored(ConsoleColor.White, $"  -> {mp.ToString()}");

            //MemoryPartitionsUid.Set(4, new MemoryPartition(InjectContext, Low: 0x8A000000, High: 0x8BC00000, Allocated: false, Name: "UMD Cache Partition")); // 28MB

            //MemoryPartitionsUid.Set(6, new MemoryPartition(InjectContext, Low: 0x8BC00000, High: 0x8C000000, Allocated: false, Name: "ME Partition")); // 4MB
        }
    }
}