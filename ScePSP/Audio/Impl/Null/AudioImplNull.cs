using System;

namespace ScePSP.Core.Audio.Impl.Null
{
    public class AudioImplNull : PspAudioImpl
    {
        public override void Update(Action<short[]> readStream)
        {
        }

        public override void StopSynchronized()
        {
        }

        public override bool IsWorking => true;
    }
}