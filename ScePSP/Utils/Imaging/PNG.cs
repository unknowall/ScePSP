using ScePSPUtils.Extensions;
using System;
using System.IO;

namespace ScePSP.Utils.Imaging
{
    public class PNG
    {
        static public byte[] Encode(Bitmap32 bitmap)
        {
            var stream = new MemoryStream();
            stream.WriteString("test");
            throw new Exception("WIP");
        }
    }
}