using ScePSPUtils.Streams;
using System.IO;

namespace ScePSP.Hle.Formats.Pgd
{
    public class PgdProxyStream : ProxyStream
    {
        public PgdProxyStream(Stream baseStream) : base(baseStream)
        {
        }
    }
}