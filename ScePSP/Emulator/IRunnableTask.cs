namespace ScePSP.Runner.Tasks
{
    public interface IRunnableTask
    {
        void StartSynchronized(bool ForceRun = false);

        void StopSynchronized();

        void PauseSynchronized();

        void ResumeSynchronized();
    }
}