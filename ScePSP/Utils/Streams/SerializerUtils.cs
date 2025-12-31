using ScePSPUtils.Extensions;
using System;
using System.IO;

namespace ScePSPUtils.Streams
{
    /// <summary>
    /// 
    /// </summary>
    public static class SerializerUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static MemoryStream SerializeToMemoryStream(Action<Stream> serializer)
        {
            var stream = new MemoryStream();
            stream.PreservePositionAndLock(() =>
            {
                serializer(stream);
                stream.Flush();
            });
            return stream;
        }
    }
}