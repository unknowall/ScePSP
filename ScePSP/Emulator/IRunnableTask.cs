namespace ScePSP.Runner.Tasks
{
    public interface IRunnableTask
    {
        void StartSynchronized();

        void StopSynchronized();

        void PauseSynchronized();

        void ResumeSynchronized();
    }
}