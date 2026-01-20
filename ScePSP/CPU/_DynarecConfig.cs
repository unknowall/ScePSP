namespace ScePSP.Cpu
{
    public class _DynarecConfig
    {
        public const bool UpdatePCEveryInstruction = false;

        public const bool FunctionCallWithStaticReferences = true;

        public const bool EnableFastPspMemoryUtilsGetFastMemoryReader = false;

        public const bool AllowFastMemory = true;

        public const bool EmitCallTick = false;

        public const bool EnableTailCalling = true;

        public const bool BranchFlagAsLocal = true;

        public const bool DebugFunctionCreation = false;

        public const bool EnableGpuSignalsCallback = true;

        public const bool EnableGpuFinishCallback = false;

        public const bool ImmediateLinking = true;

        public const bool AllowCreatingUsedFunctionsInBackground = true;

        public const bool DisableDotNetJitOptimizations = false;

        public const bool ForceJitOptimizationsOnEvenLargeFunctions = true;

        //public const int InstructionCountToDisableOptimizations = 500;
        //public const int InstructionCountToDisableOptimizations = 200;
        public const int InstructionCountToDisableOptimizations = 100;
    }
}
