using System;

namespace ScePSP.Core.AudioBackEnd
{
    public abstract class AudioBackEnd
    {
        public abstract void Update(Action<short[]> ReadStream);

        public abstract void Pause();

        public abstract void Resume();

        public abstract void Stop();

    }
}