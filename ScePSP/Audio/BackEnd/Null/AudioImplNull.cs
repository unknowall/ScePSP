using System;

namespace ScePSP.Core.AudioBackEnd.Null
{
    public class AudioImplNull : AudioBackEnd
    {
        public override void Update(Action<short[]> readStream)
        {
        }

        public override void StopSynchronized()
        {
        }
    }
}