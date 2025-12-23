using System;
using System.Runtime.Serialization;

namespace ScePSPUtils
{
#pragma warning disable SYSLIB0050
#pragma warning disable SYSLIB0051
    public class StackTraceUtils
    {
        public static void PreserveStackTrace(Exception e)
        {
            var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
            var mgr = new ObjectManager(null, ctx);
            var si = new SerializationInfo(e.GetType(), new FormatterConverter());

            e.GetObjectData(si, ctx);
            mgr.RegisterObject(e, 1, si); // prepare for SetObjectData
            mgr.DoFixups(); // ObjectManager calls SetObjectData

            // voila, e is unmodified save for _remoteStackTraceString
        }
    }
#pragma warning restore SYSLIB0050
#pragma warning restore SYSLIB0051
}