using System.Diagnostics;
using Spectre.Console;

namespace RealProxy;

class DynamicProxy<T> : System.Runtime.Remoting.Proxies.RealProxy
{
    private readonly T _decorated;

    public DynamicProxy(T decorated)
        : base(typeof(T))
    {
        _decorated = decorated;
    }

    public override IMessage Invoke(IMessage msg)
    {
        var methodCall = msg as IMethodCallMessage;
        var methodInfo = methodCall.MethodBase as MethodInfo;
        AnsiConsole.MarkupLine("[bold yellow]Before executing '{0}'[/]", methodCall.MethodName);
        try
        {
            var result = methodInfo.Invoke(_decorated, methodCall.InArgs);
            //如果是异步方法
            if (methodInfo.IsAsyncMethod())
            {
                var task = (Task)result;
                //如果是无返回值的异步方法
                if (methodInfo.ReturnType == typeof(Task))
                {
                    var val =InternalAsyncHelper. AwaitTaskWithPostActionAndFinally(
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
                    return new ReturnMessage(
                        val,
                        null,
                        0,
                        methodCall.LogicalCallContext,
                        methodCall
                    );
                }
                else
                {
                    //如果是有返回值的异步方法
                    var returnTypeGenericTypeArgument = methodInfo.ReturnType.GenericTypeArguments[0];
                    var val = InternalAsyncHelper.CallAwaitTaskWithPostActionAndFinallyAndGetResult(
                        returnTypeGenericTypeArgument,
                        result,
                        async (o) =>
                        {
                            Console.WriteLine($"Task {methodInfo.Name} completed with result {o}");
                        }, /*成功时执行*/
                        ex =>
                        {
                            if (ex != null)
                            {
                                Console.WriteLine(
                                    $"Task {methodInfo.Name} threw an exception: {ex.ToString()}"
                                );
                            }
                        }
                    );
                    return new ReturnMessage(
                        val,
                        null,
                        0,
                        methodCall.LogicalCallContext,
                        methodCall
                    );
                }
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[bold green]After executing '{{0}}' [/]",
                    methodCall.MethodName
                );
                return new ReturnMessage(
                    result,
                    null,
                    0,
                    methodCall.LogicalCallContext,
                    methodCall
                );
            }
        }
        catch (Exception e)
        {
            var str = $"Exception {e} occurred while executing {methodInfo.Name}";
            AnsiConsole.MarkupLine($"[red]Exception:[/]");
            Console.WriteLine(str);

            return new ReturnMessage(e, methodCall);
        }
    }
}
