using SafeILGenerator.Ast.Nodes;
using SafeILGenerator.Utils;
using ScePSP.Cpu.Dynarec;
using System;
using System.Runtime.CompilerServices;

namespace ScePSP.Cpu.InstructionCache
{
    public sealed class MethodCacheInfo
    {
        static public readonly MethodCacheInfo Methods = new MethodCacheInfo();

        private DynarecFunction _DynarecFunction;

        private Action<CpuThreadState> FunctionDelegate;

        public DynarecFunction DynarecFunction
        {
            get
            {
                return _DynarecFunction;
            }
        }

        public void SetDynarecFunction(DynarecFunction DynarecFunction)
        {
            this._DynarecFunction = DynarecFunction;
            this.FunctionDelegate = DynarecFunction.Delegate;
            this.StaticField.Value = DynarecFunction.Delegate;
        }

        public bool HasSpecialName
        {
            get
            {
                return (DynarecFunction != null) && !String.IsNullOrEmpty(DynarecFunction.Name);
            }
        }

        public string Name
        {
            get
            {
                if (HasSpecialName) return DynarecFunction.Name;
                return String.Format("0x{0:X8}", EntryPC);
            }
        }

        public MethodCache MethodCache;

        public ILInstanceHolderPoolItem<Action<CpuThreadState>> StaticField;

        public bool FollowPspCallingConventions;

        private MethodCacheInfo()
        {
        }

        public MethodCacheInfo(MethodCache MethodCache, Action<CpuThreadState> DelegateGeneratorForPC, uint PC)
        {
            this.MethodCache = MethodCache;
            this.FunctionDelegate = DelegateGeneratorForPC;
            this.StaticField = ILInstanceHolder.TAlloc<Action<CpuThreadState>>(DelegateGeneratorForPC);
            this.PC = PC;
        }

        public uint PC;

        public uint EntryPC { get { return DynarecFunction.EntryPC; } }

        public uint MinPC { get { return DynarecFunction.MinPC; } }

        public uint MaxPC { get { return DynarecFunction.MaxPC; } }

        public uint TotalInstructions { get { return (DynarecFunction.MaxPC - DynarecFunction.MinPC) / 7; } }

        public AstNodeStm AstTree { get { return DynarecFunction != null ? DynarecFunction.AstNode : null; } }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CallDelegate(CpuThreadState CpuThreadState)
        {
            FunctionDelegate(CpuThreadState);
        }

        public void Free()
        {
            MethodCache.Free(this);
        }
    }
}
