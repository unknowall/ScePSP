namespace ScePSP.Core.Cpu
{
    public class DynarecConfig
    {
        public static bool UpdatePCEveryInstruction = false;

        public static bool FunctionCallWithStaticReferences = true;

        public static bool EnableFastPspMemoryUtilsGetFastMemoryReader = false;

        public static bool AllowFastMemory = true;

        public static bool EmitCallTick = true;

        public static bool EnableTailCalling = true;

        public static bool BranchFlagAsLocal = true;

        public static bool DebugFunctionCreation = false;

        public static bool EnableGpuSignalsCallback = false;

        public static bool EnableGpuFinishCallback = false;

        public static bool ImmediateLinking = true;

        //Cause sometimes "System.InvalidProgramException: Common Language Runtime detect."
        public static bool AllowCreatingUsedFunctionsInBackground = true;

        public static bool DisableDotNetJitOptimizations = false;

        public static bool ForceJitOptimizationsOnEvenLargeFunctions = true;

        //public static int InstructionCountToDisableOptimizations = 500;
        //public static int InstructionCountToDisableOptimizations = 200;
        //public static int InstructionCountToDisableOptimizations = 100;
        public static int InstructionCountToDisableOptimizations = 100;
    }
}