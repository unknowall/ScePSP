namespace ScePSP.Cpu
{
    public class DynarecConfig
    {
        public static bool UpdatePCEveryInstruction = false;

        public static bool FunctionCallWithStaticReferences = true;

        public static bool EnableFastPspMemoryUtilsGetFastMemoryReader = false;

        public static bool AllowFastMemory = true;

        public static bool EmitCallTick = false;

        public static bool EnableTailCalling = true;

        public static bool BranchFlagAsLocal = true;

        public static bool DebugFunctionCreation = false;

        public static bool EnableGpuSignalsCallback = true;

        public static bool EnableGpuFinishCallback = false;

        public static bool ImmediateLinking = true;

        public static bool AllowCreatingUsedFunctionsInBackground = true;

        public static bool DisableDotNetJitOptimizations = false;

        //public static int InstructionCountToDisableOptimizations = 500;
        //public static int InstructionCountToDisableOptimizations = 200;
        public static int InstructionCountToDisableOptimizations = 100;
    }
}
