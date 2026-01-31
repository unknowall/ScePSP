using ScePSP.Memory;

namespace ScePSP.Hle.Managers
{
    public enum MemoryPartitions : int
    {
        Kernel0 = 0,
        Kernel1 = 1,
        User = 2,
        VolatilePartition = 3,
        UMD = 4,
        ME = 5,
        UserStacks = 12,
    }

    public class HleMemoryManager : IContextInitialize
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

        [Context]
        public PspMemory Memory;

        [Context]
        PspContext PspContext;

        public HleUidPool<MemoryPartition> MemoryPartitionsUid = new HleUidPool<MemoryPartition>();

        public MemoryPartition GetPartition(MemoryPartitions Partition)
        {
            return MemoryPartitionsUid.Get((int)Partition);
        }

        private HleMemoryManager()
        {
        }

        void IContextInitialize.Initialize()
        {
            MemoryPartitionsUid.Set(0, new MemoryPartition(PspContext, 0x88000000, 0x88400000, false, "Kernel Partition 1", null));
            MemoryPartitionsUid.Set(1, new MemoryPartition(PspContext, 0x88400000, 0x88580000, false, "Kernel Partition 2", null));
            MemoryPartitionsUid.Set(2, new MemoryPartition(PspContext, 0x08800000, PspMemory.MainSegment.High, false, "User Partition", null));
            MemoryPartitionsUid.Set(12, new MemoryPartition(PspContext, 0x08800000, 0x0B000000, false, "User Stacks Partition", null));
            MemoryPartitionsUid.Set(3, new MemoryPartition(PspContext, 0x08400000, 0x08800000, false, "Volatile Partition", null));
            MemoryPartitionsUid.Set(4, new MemoryPartition(PspContext, 0x8A000000, 0x8C800000, false, "UMD Cache Partition", null));
            MemoryPartitionsUid.Set(5, new MemoryPartition(PspContext, 0x8C800000, 0x8CD00000, false, "ME Partition", null));
        }
    }
}
