using System;

namespace ScePSP.Core.AudioBackEnd.Null
{
    public class NullAudio : AudioBackEnd
    {
        public override void Update(Action<short[]> readStream)
        {
        }

        public override void Pause()
        {
        }

        public override void Resume()
        {
        }

        public override void Stop()
        {
        }
    }
}