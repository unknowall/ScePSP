using System;
using System.Threading;

public class ThreadEX
{
    private readonly Thread _innerThread;
    private volatile int _abortFlag;

    public ThreadEX(ThreadStart start)
    {
        _abortFlag = 0;
        _innerThread = new Thread(() =>
        {
            while (!ShouldAbort())
            {
                try { start(); }
                catch { break; }
            }
        });
    }

    public ThreadEX(ParameterizedThreadStart start)
    {
        _abortFlag = 0;
        _innerThread = new Thread(param =>
        {
            while (!ShouldAbort())
            {
                try { start(param); }
                catch { break; }
            }
        });
    }

    public void Abort() => Interlocked.Exchange(ref _abortFlag, 1);

    public bool ShouldAbort() => Interlocked.CompareExchange(ref _abortFlag, 0, 0) == 1;

    public void Start() => _innerThread.Start();

    public void Start(object parameter) => _innerThread.Start(parameter);

    public void Join() => _innerThread.Join();

    public int ManagedThreadId => _innerThread.ManagedThreadId;

    public bool IsAlive => _innerThread.IsAlive;

    public string Name
    {
        get => _innerThread.Name;
        set => _innerThread.Name = value;
    }

    public static Thread CurrentThread => Thread.CurrentThread;

    public bool IsBackground
    {
        get => _innerThread.IsBackground;
        set => _innerThread.IsBackground = value;
    }
}