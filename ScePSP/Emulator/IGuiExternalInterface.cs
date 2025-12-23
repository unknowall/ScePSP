using System;

namespace ScePSP.Core
{
    public interface IGuiExternalInterface
    {
        InjectContext InjectContext { get; }

        void LoadFile(string FileName);

        void Pause();

        void Resume();

        void PauseResume(Action Action);

        bool IsPaused();

        void ShowDebugInformation();

        void CaptureGpuFrame();
    }
}