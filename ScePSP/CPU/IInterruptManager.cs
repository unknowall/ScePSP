namespace ScePSP.Cpu
{
    public interface IInterruptManager
    {
        void Interrupt(CpuThreadState CpuThreadState);
    }
}
