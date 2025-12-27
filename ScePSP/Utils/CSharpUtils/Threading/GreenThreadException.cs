using System;

namespace ScePSPUtils.Threading
{
    public class GreenThreadException : Exception
    {
        public GreenThreadException(string name, Exception innerException) : base(name, innerException)
        {
        }
    }
}