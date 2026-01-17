using System;

namespace ScePSP.Cpu
{
    public class PspBreakException : Exception
    {
        public PspBreakException(string Message) : base(Message) { }
    }
}
