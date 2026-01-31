namespace ScePSP.GE
{
    public interface IGEConnector
    {
        void Signal(uint PC, GeCallbackData GeCallbackData, uint Signal, SignalBehavior Behavior, bool ExecuteNow);
        void Finish(uint PC, GeCallbackData GeCallbackData, uint Arg, bool ExecuteNow);
    }
}
