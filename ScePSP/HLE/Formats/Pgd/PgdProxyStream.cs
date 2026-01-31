using ScePSPUtils.Streams;
using System.IO;

namespace ScePSP.Hle.Pgd
{
    public class PgdProxyStream : ProxyStream
    {
        public PgdProxyStream(Stream BaseStream) : base(BaseStream)
        {
        }
    }
}
