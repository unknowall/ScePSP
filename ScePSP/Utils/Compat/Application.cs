using System;

namespace ScePSP.Compat
{
    public class Application
    {
        public static string ExecutablePath => AppDomain.CurrentDomain.BaseDirectory;
    }
}