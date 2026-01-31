using System;

namespace ScePSP.Audio
{
    public class NullAudio : AudioBackEnd
    {
        public NullAudio()
        {
        }

        public override void Update(Action<short[]> ReadStream)
        {
        }

        public override void StopSynchronized()
        {
        }
    }
}
