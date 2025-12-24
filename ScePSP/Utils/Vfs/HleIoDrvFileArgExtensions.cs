using ScePSP.Hle.Vfs;
using System.IO;

namespace System
{
    public static class HleIoDrvFileArgExtensions
    {
        public static Stream GetStream(this HleIoDrvFileArg HleIoDrvFileArg)
        {
            return new FileHandle(HleIoDrvFileArg);
        }
    }
}