using ContextBoundObjectDemo;

/// <summary>
/// 记录方法进入日志与退出日志
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class LoggingInterceptorAttribute : WorkerAttribute
{
    static readonly LoggingWorker Instance = new LoggingWorker();

    public string str = "";

    /// <summary>
    /// 记录方法进入日志与退出日志
    /// </summary>
    /// <param name="str">附加信息</param>
    public LoggingInterceptorAttribute(string str = "")
    {
        this.str = str;
    }

    public override void DoWorkAfter()
    {
        Instance.After(str);
    }

    public override void DoWorkBefore()
    {
        Instance.Before(str);
    }

    public override IDualWorkerService GetWorker()
    {
        return Instance;
    }
}