namespace ScePSP.GE
{
    public enum SyncTypeEnum : byte
    {
        WaitForCompletion = 0,
        Peek = 1,
    }

    public enum TextureLevelMode { Auto = 0, Const = 1, Slope = 2 }

    /*
	 *   - GU_SYNC_FINISH - 0 - Wait until the last sceGuFinish command is reached
	 *   - GU_SYNC_SIGNAL - 1 - Wait until the last (?) signal is executed
	 *   - GU_SYNC_DONE   - 2 - Wait until all commands currently in list are executed
	 *   - GU_SYNC_LIST   - 3 - Wait for the currently executed display list (GU_DIRECT)
	 *   - GU_SYNC_SEND   - 4 - Wait for the last send list
	 *   
	 *   int sceGuSync(int mode, SyncTypeEnum what)
	 *	 {
	 *		 switch (mode)
	 *		 {
	 *			 case GU_SYNC_FINISH: return sceGeDrawSync(what);
	 *			 case GU_SYNC_LIST  : return sceGeListSync(ge_list_executed[0], what);
	 *			 case GU_SYNC_SEND  : return sceGeListSync(ge_list_executed[1], what);
	 *		 	 default: case GU_SYNC_SIGNAL: case GU_SYNC_DONE: return 0;
	 *	 	 }
	 *	 }
	 */
    public enum GUSyncTypeEnum : uint
    {
        ListDone = 0,

        ListQueued = 1,

        ListDrawingDone = 2,

        ListStallReached = 3,

        ListCancelDone = 4,
    }

    public enum SignalBehavior : byte
    {
        PSP_GE_SIGNAL_NONE = 0x00,
        PSP_GE_SIGNAL_HANDLER_SUSPEND = 0x01,
        PSP_GE_SIGNAL_HANDLER_CONTINUE = 0x02,
        PSP_GE_SIGNAL_HANDLER_PAUSE = 0x03,
        PSP_GE_SIGNAL_SYNC = 0x08,
        PSP_GE_SIGNAL_JUMP = 0x10,
        PSP_GE_SIGNAL_CALL = 0x11,
        PSP_GE_SIGNAL_RET = 0x12,
        PSP_GE_SIGNAL_RJUMP = 0x13,
        PSP_GE_SIGNAL_RCALL = 0x14,
        PSP_GE_SIGNAL_OJUMP = 0x15,
        PSP_GE_SIGNAL_OCALL = 0x16,

        PSP_GE_SIGNAL_RTBP0 = 0x20,
        PSP_GE_SIGNAL_RTBP1 = 0x21,
        PSP_GE_SIGNAL_RTBP2 = 0x22,
        PSP_GE_SIGNAL_RTBP3 = 0x23,
        PSP_GE_SIGNAL_RTBP4 = 0x24,
        PSP_GE_SIGNAL_RTBP5 = 0x25,
        PSP_GE_SIGNAL_RTBP6 = 0x26,
        PSP_GE_SIGNAL_RTBP7 = 0x27,
        PSP_GE_SIGNAL_OTBP0 = 0x28,
        PSP_GE_SIGNAL_OTBP1 = 0x29,
        PSP_GE_SIGNAL_OTBP2 = 0x2A,
        PSP_GE_SIGNAL_OTBP3 = 0x2B,
        PSP_GE_SIGNAL_OTBP4 = 0x2C,
        PSP_GE_SIGNAL_OTBP5 = 0x2D,
        PSP_GE_SIGNAL_OTBP6 = 0x2E,
        PSP_GE_SIGNAL_OTBP7 = 0x2F,
        PSP_GE_SIGNAL_RCBP = 0x30,
        PSP_GE_SIGNAL_OCBP = 0x38,
        PSP_GE_SIGNAL_BREAK1 = 0xF0,
        PSP_GE_SIGNAL_BREAK2 = 0xFF,
    }

    public enum GEStatusEnum
    {
        // The list has been completed (PSP_GE_LIST_COMPLETED)
        Completed = 0,

        //  list is queued but not executed yet (PSP_GE_LIST_QUEUED)
        Queued = 1,

        // The list is currently being executed (PSP_GE_LIST_DRAWING)
        Drawing = 2,

        // The list was stopped because it encountered stall address (PSP_GE_LIST_STALL_REACHED)
        StallReached = 3,

        // The list is paused because of a signal or sceGeBreak PSP_GE_LIST_END_REACHED
        EndReached = 4,

        //PSP_GE_LIST_CANCEL_DONE
        Cancel = 5
    }

    public struct OptionalParams
    {
        public int ContextAddress;
        public int StackDepth;
        public int StackAddress;
    }

    unsafe public struct PspGeStack
    {
        public fixed uint Stack[8];
    }

    unsafe public struct PspGeListArgs
    {
        public uint Size;

        public uint GpuStateStructAddress;

        public uint NumberOfStacks;

        public uint StacksAddress;
    }
}
