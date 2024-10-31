using System.Diagnostics;
using System.Reflection;
using RealProxy;

public class LoggingDecorator<T> : DispatchProxy
{
    private T _decorated;

    protected override object Invoke(MethodInfo methodInfo, object[] args)
    {
        try
        {
            LogBefore(methodInfo, args);

            var result = methodInfo.Invoke(_decorated, args);
            if (methodInfo.IsAsyncMethod())
            {
                if (methodInfo.ReturnType == typeof(Task))
                {
                    var task = (Task)result;
                    var val = InternalAsyncHelper.AwaitTaskWithPostActionAndFinally(
                        task,
                        async () =>
                        {
                            Debug.WriteLine($"Task {methodInfo.Name} completed");
                        }, /*成功时执行*/
                        ex =>
                        {
                            if (ex != null)
                            {
                                Debug.WriteLine(
                                    $"Task {methodInfo.Name} threw an exception: {ex.ToString()}"
                                );
                            }
                        }
                    );
                    return val;
                }
                else
                {
                    var returnTypeGenericTypeArgument = methodInfo.ReturnType.GenericTypeArguments[
                        0
                    ];
                    var val = InternalAsyncHelper.CallAwaitTaskWithPostActionAndFinallyAndGetResult(
                        returnTypeGenericTypeArgument,
                        result,
                        async (o) =>
                        {
                            Debug.WriteLine($"Task {methodInfo.Name} completed with result {o}");
                        }, /*成功时执行*/
                        ex =>
                        {
                            if (ex != null)
                            {
                                Debug.WriteLine(
                                    $"Task {methodInfo.Name} threw an exception: {ex.ToString()}"
                                );
                            }
                        }
                    );
                    return val;
                }
            }
            else
            {
                LogAfter(methodInfo, args, result);
            }

            return result;
        }
        catch (Exception ex) when (ex is TargetInvocationException)
        {
            LogException(ex.InnerException ?? ex, methodInfo);
            throw ex.InnerException ?? ex;
        }
    }

    public static T Create(T decorated)
    {
        object proxy = Create<T, LoggingDecorator<T>>();
        ((LoggingDecorator<T>)proxy).SetParameters(decorated);

        return (T)proxy;
    }

    private void SetParameters(T decorated)
    {
        if (decorated == null)
        {
            throw new ArgumentNullException(nameof(decorated));
        }
        _decorated = decorated;
    }

    private void LogException(Exception exception, MethodInfo methodInfo = null)
    {
        Console.WriteLine(
            $"Class {_decorated.GetType().FullName}, Method {methodInfo.Name} threw exception:\n{exception}"
        );
    }

    private void LogAfter(MethodInfo methodInfo, object[] args, object result)
    {
        Console.WriteLine(
            $"Class {_decorated.GetType().FullName}, Method {methodInfo.Name} executed, Output: {result}"
        );
    }

    private void LogBefore(MethodInfo methodInfo, object[] args)
    {
        Console.WriteLine(
            $"Class {_decorated.GetType().FullName}, Method {methodInfo.Name} is executing"
        );
    }
}
