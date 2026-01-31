using ScePSP.Cpu;
using ScePSP.Hle.Managers;
using ScePSP.Memory;
using ScePSP.Runner;
using ScePSPUtils;
using System;
using System.Collections.Generic;

namespace ScePSP.Hle
{
    public class HleInterop
    {
        [Context]
        protected HleThreadManager HleThreadManager;

        [Context]
        protected CpuProcessor CpuProcessor;

        [Context]
        protected PSP PspContext;

        private Queue<HleInterop.QueuedExecution> QueuedExecutions = new Queue<HleInterop.QueuedExecution>();

        private HleInterop()
        {
        }

        public uint ExecuteFunctionNowLater(uint Function, bool ExecuteNow, params object[] Arguments)
        {
            if (ExecuteNow)
            {
                return this.ExecuteFunctionNow(Function, Arguments);
            }
            this.ExecuteFunctionLater(Function, Arguments);
            return 0u;
        }

        public uint ExecuteFunctionNow(uint Function, params object[] Arguments)
        {
            HleThread currentOrAny = this.HleThreadManager.CurrentOrAny;
            currentOrAny.CpuThreadState.CopyRegistersFrom(this.HleThreadManager.CurrentOrAny.CpuThreadState);
            HleInterop.SetArgumentsToCpuThreadState(currentOrAny.CpuThreadState, Function, Arguments);
            //Console.Out.WriteLineColored(ConsoleColor.Yellow, "ExecuteFunctionNow: 0x{0:X8}", Function);
            currentOrAny.CpuThreadState.ExecuteFunctionAndReturn(currentOrAny.CpuThreadState.PC);
            //Console.Out.WriteLineColored(ConsoleColor.Yellow, "0x{0:X8}... GPR2: 0x{1:X}", Function, currentOrAny.CpuThreadState.GPR2);
            return currentOrAny.CpuThreadState.GPR2;
        }

        public bool HasQueuedFunctions
        {
            get
            {
                return this.QueuedExecutions.Count > 0;
            }
        }

        public int ExecuteAllQueuedFunctionsNow()
        {
            int result;
            lock (this.QueuedExecutions)
            {
                int num = 0;
                while (this.QueuedExecutions.Count > 0)
                {
                    HleInterop.QueuedExecution queuedExecution = this.QueuedExecutions.Dequeue();
                    uint obj = this.ExecuteFunctionNow(queuedExecution.Function, queuedExecution.Arguments);
                    if (queuedExecution.ExecutedCallback != null)
                    {
                        queuedExecution.ExecutedCallback(obj);
                    }
                    num++;
                }
                result = num;
            }
            return result;
        }

        public void ExecuteFunctionLater(uint Function, params object[] Arguments)
        {
            this.ExecuteFunctionLater(Function, delegate (uint Result)
            {
            }, Arguments);
        }

        public void ExecuteFunctionLater(uint Function, Action<uint> ExecutedCallback, params object[] Arguments)
        {
            lock (this.QueuedExecutions)
            {
                this.QueuedExecutions.Enqueue(new HleInterop.QueuedExecution
                {
                    Function = Function,
                    ExecutedCallback = ExecutedCallback,
                    Arguments = Arguments
                });
            }
        }

        public HleThread Execute(CpuThreadState FakeCpuThreadState)
        {
            HleThread currentOrAny = this.HleThreadManager.CurrentOrAny;
            currentOrAny.CpuThreadState.CopyRegistersFrom(FakeCpuThreadState);
            currentOrAny.CpuThreadState.ExecuteAT(currentOrAny.CpuThreadState.PC);
            return currentOrAny;
        }

        public static void SetArgumentsToCpuThreadState(CpuThreadState CpuThreadState, uint Function, params object[] Arguments)
        {
            int GprIndex = 4;
            Action<int> action = delegate (int Alignment)
            {
                GprIndex = (int)MathUtils.NextAligned((uint)GprIndex, Alignment);
            };
            foreach (object obj in Arguments)
            {
                Type type = obj.GetType();
                if (type == typeof(uint))
                {
                    action(1);
                    CpuThreadState.GPR[GprIndex++] = (int)((uint)obj);
                }
                else if (type == typeof(int))
                {
                    action(1);
                    CpuThreadState.GPR[GprIndex++] = (int)obj;
                }
                else if (type == typeof(PspPointer))
                {
                    action(1);
                    CpuThreadState.GPR[GprIndex++] = (int)(uint)((PspPointer)obj);
                }
                else
                {
                    if (!type.IsEnum)
                    {
                        throw new NotImplementedException(string.Format("Can't handle type '{0}'", type));
                    }
                    action(1);
                    CpuThreadState.GPR[GprIndex++] = Convert.ToInt32(obj);
                }
            }
            CpuThreadState.PC = Function;
        }

        public class QueuedExecution
        {
            public uint Function;

            public Action<uint> ExecutedCallback;

            public object[] Arguments;
        }
    }
}
