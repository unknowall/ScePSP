namespace ScePSP.GE
{
    public interface IGEConnector
    {
        void Signal(uint PC, GeCallbackData PspGeCallbackData, uint Signal, SignalBehavior Behavior, bool ExecuteNow);
        void Finish(uint PC, GeCallbackData PspGeCallbackData, uint Arg, bool ExecuteNow);
    }
}
