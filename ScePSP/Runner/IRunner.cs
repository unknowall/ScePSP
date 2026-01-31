namespace ScePSP.Runner
{
    public interface IRunner
    {
        void StartSynchronized();

        void StopSynchronized();

        void PauseSynchronized();

        void ResumeSynchronized();
    }
}
