using LightGL.DynamicLibrary;
using SafeILGenerator.Ast.Generators;
using ScePSP.Core;
using ScePSP.Cpu.Dynarec;
using ScePSP.Cpu.Emitter;
using ScePSP.Memory;
using ScePSPUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ScePSP.Cpu.InstructionCache
{
    public sealed class MethodCache : IContextInitialize
    {
        public static readonly MethodCache Methods = new MethodCache();

        private static readonly AstMipsGenerator ast = AstMipsGenerator.Instance;
        private static readonly GeneratorIL GeneratorILInstance = new GeneratorIL();

        public readonly Dictionary<uint, MethodCacheInfo> MethodMapping = new Dictionary<uint, MethodCacheInfo>(64 * 1024);
        public Dictionary<uint, DynarecFunction> Functions = new Dictionary<uint, DynarecFunction>();
        public IEnumerable<uint> PCs { get { return MethodMapping.Keys; } }
        public IEnumerable<uint> FUNCs { get { return Functions.Keys; } }

        [Context]
        public CpuProcessor CpuProcessor;

        [Context]
        public PspStoredConfig StoredConfig;

        private Thread Thread;
        public bool Runing;

        private ThreadMessageBus<uint> ExploreQueue = new ThreadMessageBus<uint>();
        AutoResetEvent CompletedFunction = new AutoResetEvent(false);

        void IContextInitialize.Initialize()
        {
            Runing = true;
            Thread = new Thread(ThreadMain);
            Thread.Name = "DynaracThread";
            Thread.Start();
        }

        public void Dispose()
        {
            Runing = false;
            ExploreQueue.Add(0xDEAD);
        }

        public MethodCacheInfo GetForPC(uint PC)
        {
            if (!Runing) throw new OperationCanceledException("Stop CPU Core");

            if (MethodMapping.ContainsKey(PC))
            {
                return MethodMapping[PC];
            }
            else
            {
                var DelegateGeneratorForPC = GeneratorILInstance.GenerateDelegate<Action<CpuThreadState>>(
                    "MethodCache.DynamicCreateNewFunction",

                    ast.Statements(
                        ast.Statement(
                            ast.CallInstance(
                                ast.CpuThreadState, (Action<MethodCacheInfo, uint>)CpuThreadState.Methods.GetFunc, ast.GetMethodCacheInfoAtPC(PC), PC)),

                    ast.Statement(
                        ast.TailCall(
                            ast.CallInstance(
                                ast.GetMethodCacheInfoAtPC(PC), (Action<CpuThreadState>)MethodCacheInfo.Methods.CallDelegate, ast.CpuThreadState))),

                    ast.Return()
                ));
                return MethodMapping[PC] = new MethodCacheInfo(this, DelegateGeneratorForPC, PC);
            }
        }

        internal void Free(MethodCacheInfo MethodCacheInfo)
        {
            MethodMapping.Remove(MethodCacheInfo.EntryPC);
        }

        public void FlushAll()
        {
            foreach (var MethodCacheInfo in MethodMapping.Values.ToArray())
            {
                MethodCacheInfo.Free();
            }

            Functions.Clear();
        }

        public void FlushRange(uint Start, uint End)
        {
            foreach (var MethodCacheInfo in MethodMapping.Values.ToArray())
            {
                if (MethodCacheInfo.MaxPC >= Start && MethodCacheInfo.MinPC <= End)
                {
                    MethodCacheInfo.Free();
                }
            }
        }

        public void GetFunc(CpuThreadState CpuThreadState, MethodCacheInfo MethodCacheInfo, uint PC)
        {
            if (!this.Functions.ContainsKey(PC))
            {
                var Func = CpuProcessor.DynarecFunctionCompiler.CreateFunction(new InstructionStreamReader(new PspMemoryStream(CpuProcessor.Memory)), PC);

                Functions[PC] = Func;

                if (DynarecConfig.AllowCreatingUsedFunctionsInBackground)
                    foreach (var callpc in Func.CallingPCs) AddQueue(callpc);
            }
            MethodCacheInfo.SetDynarecFunction(Functions[PC]);
        }

        private void ThreadMain()
        {
            while (Runing)
            {
                var PC = ExploreQueue.ReadOne();
                if (_ShouldAdd(PC) && Runing)
                {
                    var DynarecFunction = _GenerateForPC(PC);
                    lock (this) this.Functions[PC] = DynarecFunction;
                }
                CompletedFunction.Set();
            }
        }

        private bool _ShouldAdd(uint PC)
        {
            return !Functions.ContainsKey(PC) && !MethodMapping.ContainsKey(PC) && PspMemory.IsAddressValid(PC);
        }

        public void AddQueue(uint PC)
        {
            ExploreQueue.Add(PC);
        }

        private DynarecFunction _GenerateForPC(uint PC)
        {
            var Memory = CpuProcessor.Memory;

            if (DynarecConfig.DebugFunctionCreation)
            {
                Console.Write("PC=0x{0:X8}...", PC);
            }

            var Time0 = DateTime.UtcNow;

            var DynarecFunction = CpuProcessor.DynarecFunctionCompiler.CreateFunction(new InstructionStreamReader(new PspMemoryStream(Memory)), PC);
            if (DynarecFunction.EntryPC != PC) throw (new Exception("Unexpected error"));

            var Time1 = DateTime.UtcNow;

            if (DynarecConfig.ImmediateLinking)
            {
                try
                {
                    if (Platform.IsMono) Marshal.Prelink(DynarecFunction.Delegate.Method);
                    DynarecFunction.Delegate(null);
                }
                catch (InvalidProgramException InvalidProgramException)
                {
                    Console.Error.WriteLine("Invalid delegate:");
                    Console.Error.WriteLine(DynarecFunction.AstNode.ToCSharpString());
                    Console.Error.WriteLine(DynarecFunction.AstNode.ToILString<Action<CpuThreadState>>());
                    throw (InvalidProgramException);
                }
            }

            var Time2 = DateTime.UtcNow;
            DynarecFunction.TimeLinking = Time2 - Time1;
            var TimeAstGeneration = Time1 - Time0;

            if (DynarecConfig.DebugFunctionCreation)
            {
                ConsoleUtils.SaveRestoreConsoleColor(((TimeAstGeneration + DynarecFunction.TimeLinking).TotalMilliseconds > 10) ? ConsoleColor.Red : ConsoleColor.Gray, () =>
                {
                    Console.WriteLine(
                        "({0}): (analyze: {1}, generateAST: {2}, optimize: {3}, generateIL: {4}, createDelegate: {5}, link: {6}): ({1}, {2}, {3}, {4}, {5}, {6}) : {7} ms",
                        (DynarecFunction.MaxPC - DynarecFunction.MinPC) / 4,
                        (int)DynarecFunction.TimeAnalyzeBranches.TotalMilliseconds,
                        (int)DynarecFunction.TimeGenerateAst.TotalMilliseconds,
                        (int)DynarecFunction.TimeOptimize.TotalMilliseconds,
                        (int)DynarecFunction.TimeGenerateIL.TotalMilliseconds,
                        (int)DynarecFunction.TimeCreateDelegate.TotalMilliseconds,
                        (int)DynarecFunction.TimeLinking.TotalMilliseconds,
                        (int)(TimeAstGeneration + DynarecFunction.TimeLinking).TotalMilliseconds
                    );
                });
            }
            //DynarecFunction.AstNode = DynarecFunction.AstNode.Optimize(CpuProcessor);
            return DynarecFunction;
        }
    }

    public class ThreadMessageBus<T>
    {
        private Queue<T> Queue = new Queue<T>();
        private AutoResetEvent HasItems = new AutoResetEvent(false);

        public void Add(T item)
        {
            lock (this)
            {
                Queue.Enqueue(item);
                HasItems.Set();
            }
        }

        public T ReadOne()
        {
            HasItems.WaitOne();

            lock (this)
            {
                return Queue.Dequeue();
            }
        }
    }
}
