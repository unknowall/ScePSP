using System;

namespace ScePSP.BackEnd.NullAudio
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