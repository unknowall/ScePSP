using System;

namespace ScePSP.Core.Cpu
{
    public class PspBreakException : Exception
    {
        public PspBreakException(string message) : base(message)
        {
        }
    }
}