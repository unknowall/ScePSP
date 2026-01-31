using System;

namespace ScePSP.Audio
{
    public abstract class AudioBackEnd
    {
        public abstract void Update(Action<short[]> ReadStream);

        public abstract void StopSynchronized();
    }
}
