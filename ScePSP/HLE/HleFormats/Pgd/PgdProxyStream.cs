using System.IO;
using ScePSPUtils.Streams;

namespace ScePSP.Hle.Formats.Pgd
{
    public class PgdProxyStream : ProxyStream
    {
        public PgdProxyStream(Stream baseStream) : base(baseStream)
        {
        }
    }
}