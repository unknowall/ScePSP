namespace ScePSP.Cpu
{
    public interface ICpuConnector
    {
        void Yield(CpuThreadState CpuThreadState);
    }
}
