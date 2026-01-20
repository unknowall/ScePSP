using LightCodec.h264.decoder;
using LightGL.DynamicLibrary;
using SafeILGenerator.Ast.Generators;
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
    public sealed class MethodCache
    {
        public static readonly MethodCache Methods = new MethodCache();

        private static readonly AstMipsGenerator ast = AstMipsGenerator.Instance;
        private static readonly GeneratorIL GeneratorILInstance = new GeneratorIL();

        private readonly Dictionary<uint, MethodCacheInfo> MethodMapping = new Dictionary<uint, MethodCacheInfo>(64 * 1024);
        public IEnumerable<uint> PCs { get { return MethodMapping.Keys; } }

        public CpuProcessor CpuProcessor => PSPDrivers.CPU;

        public MethodCompilerThread MethodCompilerThread;
        public bool Runing = true;

        public MethodCache()
        {
            MethodCompilerThread = new MethodCompilerThread(CpuProcessor, this);
        }

        public MethodCacheInfo GetForPC(uint PC)
        {
            if (MethodMapping.ContainsKey(PC))
            {
                return MethodMapping[PC];
            }
            else
            {
                var DelegateGeneratorForPC = GeneratorILInstance.GenerateDelegate<Action<CpuThreadState>>("MethodCache.DynamicCreateNewFunction", ast.Statements(
                    ast.Statement(ast.CallInstance(ast.CpuThreadState, (Action<MethodCacheInfo, uint>)CpuThreadState.Methods._MethodCacheInfo_SetInternal, ast.GetMethodCacheInfoAtPC(PC), PC)),
                    ast.Statement(ast.TailCall(ast.CallInstance(ast.GetMethodCacheInfoAtPC(PC), (Action<CpuThreadState>)MethodCacheInfo.Methods.CallDelegate, ast.CpuThreadState))),
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
        }

        public void FlushRange(uint Start, uint End)
        {
            //Console.Error.WriteLine("[{0}]", MethodMapping);
            //Console.Error.WriteLine("[{0}]", MethodMapping.Values);
            foreach (var MethodCacheInfo in MethodMapping.Values.ToArray())
            //foreach (var MethodCacheInfo in MethodMapping.Values)
            {
                //Console.Error.WriteLine("[{0}]", MethodCacheInfo);
                if (MethodCacheInfo.MaxPC >= Start && MethodCacheInfo.MinPC <= End)
                {
                    MethodCacheInfo.Free();
                }
            }
        }

        public void _MethodCacheInfo_SetInternal(CpuThreadState CpuThreadState, MethodCacheInfo MethodCacheInfo, uint PC)
        {
            MethodCacheInfo.SetDynarecFunction(MethodCompilerThread.GetDynarecFunctionForPC(PC));
        }
    }

    public class ThreadMessageBus<T>
    {
        private LinkedList<T> Queue = new LinkedList<T>();
        private ManualResetEvent HasItems = new ManualResetEvent(false);

        public void AddFirst(T item)
        {
            lock (this)
            {
                Queue.AddFirst(item);
                HasItems.Set();
            }
        }

        public void AddLast(T item)
        {
            lock (this)
            {
                Queue.AddLast(item);
                HasItems.Set();
            }
        }

        public T ReadOne()
        {
            lock (this)
            {
                HasItems.WaitOne();
                var item = Queue.First.Value;
                Queue.RemoveFirst();
                if (Queue.Count == 0) HasItems.Reset();
                return item;
            }
        }
    }

    public class MethodCompilerThread
    {
        private Thread Thread;
        private Dictionary<uint, DynarecFunction> Functions = new Dictionary<uint, DynarecFunction>();
        private HashSet<uint> ExploringPCs = new HashSet<uint>();
        private ThreadMessageBus<uint> ExploreQueue = new ThreadMessageBus<uint>();
        private CpuProcessor CpuProcessor;
        private MethodCache MethodCache;

        public MethodCompilerThread(CpuProcessor CpuProcessor, MethodCache MethodCache)
        {
            this.CpuProcessor = CpuProcessor;
            this.MethodCache = MethodCache;
            this.Thread = new Thread(Main)
            {
                IsBackground = true
            };
            this.Thread.Start();
        }

        public void Clear()
        {
            Functions.Clear();
            ExploringPCs.Clear();
        }

        private bool _ShouldAdd(uint PC)
        {
            return !Functions.ContainsKey(PC) && !ExploringPCs.Contains(PC);
        }

        public void AddPCNow(uint PC)
        {
            lock (this)
            {
                if (_ShouldAdd(PC))
                {
                    ExploringPCs.Add(PC);
                    ExploreQueue.AddFirst(PC);
                }
            }
        }

        public void AddPCLater(uint PC)
        {
            lock (this)
            {
                if (_ShouldAdd(PC))
                {
                    ExploringPCs.Add(PC);
                    ExploreQueue.AddLast(PC);
                }
            }
        }

        AutoResetEvent CompletedFunction = new AutoResetEvent(false);

        private void Main()
        {
            while (MethodCache.Runing)
            {
                var PC = ExploreQueue.ReadOne();
                //Console.Write("Compiling {0:X8}...", PC);
                var DynarecFunction = _GenerateForPC(PC);
                lock (this) this.Functions[PC] = DynarecFunction;
                //Console.WriteLine("Ok");
                CompletedFunction.Set();
            }
        }

        private DynarecFunction _GenerateForPC(uint PC)
        {
            var Memory = CpuProcessor.Memory;
            if (_DynarecConfig.DebugFunctionCreation)
            {
                Console.Write("PC=0x{0:X8}...", PC);
            }

            var Time0 = DateTime.UtcNow;

            var DynarecFunction = CpuProcessor.DynarecFunctionCompiler.CreateFunction(new InstructionStreamReader(new PspMemoryStream(Memory)), PC);
            if (DynarecFunction.EntryPC != PC) throw (new Exception("Unexpected error"));

            if (_DynarecConfig.AllowCreatingUsedFunctionsInBackground)
            {
                foreach (var CallingPC in DynarecFunction.CallingPCs)
                {
                    if (PspMemory.IsAddressValid(CallingPC))
                    {
                        AddPCLater(CallingPC);
                    }
                }
            }

            var Time1 = DateTime.UtcNow;

            if (_DynarecConfig.ImmediateLinking)
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

            if (_DynarecConfig.DebugFunctionCreation)
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

        public DynarecFunction GetDynarecFunctionForPC(uint PC)
        {
            lock (this)
            {
                if (!this.Functions.ContainsKey(PC))
                {
                    this.Functions[PC] = CpuProcessor.DynarecFunctionCompiler.CreateFunction(new InstructionStreamReader(new PspMemoryStream(CpuProcessor.Memory)), PC);
                }
                return this.Functions[PC];
            }
        }
    }
}
