using ScePSP.Memory;

namespace ScePSP.Hle.Managers
{
    public enum MemoryPartitions : int
    {
        Kernel0 = 0,
        Kernel1 = 1,
        User = 2,
        UserStacks = 3,
        UMD = 4,
        VolatilePartition = 5,
        ME = 6,
    }

    public class HleMemoryManager
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

        public PspMemory Memory => PSPDrivers.PspMemory;

        public HleUidPool<MemoryPartition> MemoryPartitionsUid = new HleUidPool<MemoryPartition>();

        public MemoryPartition GetPartition(MemoryPartitions Partition)
        {
            return MemoryPartitionsUid.Get((int)Partition);
        }

        public HleMemoryManager()
        {
            MemoryPartitionsUid.Set((int)MemoryPartitions.Kernel0,
                new MemoryPartition(Low: 0x88000000, High: 0x88300000, Allocated: false, Name: "Kernel Partition 1")); // 3MB

            MemoryPartitionsUid.Set((int)MemoryPartitions.User,
                new MemoryPartition(Low: 0x08800000, High: PspMemory.MainSegment.High, Allocated: false, Name: "User Partition")); // 24MB

            MemoryPartitionsUid.Set((int)MemoryPartitions.UserStacks,
                new MemoryPartition(Low: 0x08800000, High: 0x0B000000, Allocated: false, Name: "User Stacks Partition")); // 24MB

            MemoryPartitionsUid.Set((int)MemoryPartitions.VolatilePartition,
                new MemoryPartition(Low: 0x08400000, High: 0x08800000, Allocated: false, Name: "Volatile Partition")); // 4MB

            //MemoryPartitionsUid.Set(4, new MemoryPartition(Low: 0x8A000000, High: 0x8BC00000, Allocated: false, Name: "UMD Cache Partition")); // 28MB
            //MemoryPartitionsUid.Set(6, new MemoryPartition(Low: 0x8BC00000, High: 0x8C000000, Allocated: false, Name: "ME Partition")); // 4MB
            //MemoryPartitionsUid.Set((int)MemoryPartitions.Kernel0,new MemoryPartition(Low: 0x88000000, High: 0x88300000, Allocated: false, Name: "Kernel Partition 0")); // 3MB

            //    MemoryPartitionsUid.Set((int)MemoryPartitions.Kernel1,
            //            new MemoryPartition(Low: 0x88300000, High: 0x88400000, Allocated: false,
            //            Name: "Kernel Partition 2")); // 1MB

            //    MemoryPartitionsUid.Set((int)MemoryPartitions.User,
            //            new MemoryPartition(Low: 0x08800000, High: PspMemory.MainSegment.High, Allocated: false,
            //            Name: "User Partition")); // 24MB

            //    MemoryPartitionsUid.Set((int)MemoryPartitions.UserStacks,
            //            new MemoryPartition(Low: 0x08800000, High: 0x0B000000, Allocated: false,
            //            Name: "User Stacks Partition")); // 24MB

            //    MemoryPartitionsUid.Set((int)MemoryPartitions.VolatilePartition,
            //            new MemoryPartition(Low: 0x08400000, High: 0x08800000, Allocated: false,
            //            Name: "Volatile Partition")); // 4MB

            //    MemoryPartitionsUid.Set((int)MemoryPartitions.UMD,
            //            new MemoryPartition(Low: 0x8A000000, High: 0x8C800000, Allocated: false,
            //            Name: "UMD Cache Partition")); // 28MB

            //    MemoryPartitionsUid.Set((int)MemoryPartitions.ME,
            //            new MemoryPartition(Low: 0x8C800000, High: 0x8CC00000, Allocated: false,
            //            Name: "ME Partition")); // 4MB

        }
    }
}