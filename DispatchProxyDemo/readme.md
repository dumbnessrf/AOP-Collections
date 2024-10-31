# DispatchProxy 
`NET Core`
DispatchProxy 是一个在 .NET 框架中引入的概念，特别是在 C# 语言中。它是一种特殊类型的代理，用于在运行时动态地分派方法调用到不同的实现。DispatchProxy 类位于 System.Runtime.Remoting.Proxies 命名空间中，并且是 RealProxy 类的一个派生类。

以下是 DispatchProxy 的一些关键点：

动态分派：DispatchProxy 允许开发者在运行时决定将方法调用分派给哪个对象。这使得可以在不修改现有代码的情况下，动态地改变对象的行为。

透明代理：DispatchProxy 是一种透明代理，这意味着客户端代码不需要知道代理的存在。客户端代码可以直接调用方法，就像它们直接调用实际对象一样。

类型安全：尽管 DispatchProxy 是动态分派的，但它仍然保持类型安全。代理对象可以被强制转换为任何接口类型，并且只有那些接口上的方法调用才会被分派。

创建自定义代理：开发者可以通过继承 DispatchProxy 类并重写 Invoke 方法来创建自己的代理。在 Invoke 方法中，开发者可以决定如何分派方法调用，例如，基于调用的方法名、参数或其他逻辑。

性能开销：由于 DispatchProxy 涉及到运行时的动态分派，因此可能会有一些性能开销。因此，它更适合于那些性能不是主要关注点的场景。

用途：DispatchProxy 可以用于多种场景，包括但不限于日志记录、性能监控、事务管理、安全性检查、Mock对象的创建等。


**相较于RealProxy和ContextBoundObject**，DispatchProxy的实现更加便捷
## 创建日志拦截器

```csharp
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
```
## 代理异步的帮助类

```csharp
internal static class InternalAsyncHelper
{
    public static bool IsAsyncMethod(this MethodInfo method)
    {
        return method.ReturnType == typeof(Task)
               || method.ReturnType.IsGenericType
               && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
    }

    public static async Task AwaitTaskWithPostActionAndFinally(
        Task actualReturnValue,
        Func<Task> postAction,
        Action<Exception> finalAction
    )
    {
        Exception exception = null;

        try
        {
            await actualReturnValue;
            await postAction();
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            finalAction(exception);
        }
    }

    public static async Task<T> AwaitTaskWithPostActionAndFinallyAndGetResult<T>(
        Task<T> actualReturnValue,
        Func<object, Task> postAction,
        Action<Exception> finalAction
    )
    {
        Exception exception = null;
        try
        {
            var result = await actualReturnValue;
            await postAction(result);
            return result;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            finalAction(exception);
        }
    }

    public static object CallAwaitTaskWithPostActionAndFinallyAndGetResult(
        Type taskReturnType,
        object actualReturnValue,
        Func<object, Task> action,
        Action<Exception> finalAction
    )
    {
        //AwaitTaskWithPostActionAndFinallyAndGetResult<taskReturnType>(actualReturnValue, action, finalAction);
        return typeof(InternalAsyncHelper)
            .GetMethod(
                "AwaitTaskWithPostActionAndFinallyAndGetResult",
                BindingFlags.Public | BindingFlags.Static
            )
            .MakeGenericMethod(taskReturnType)
            .Invoke(null, new object[] { actualReturnValue, action, finalAction });
    }
}
```
## 声明及调用示例

```csharp
public interface ICalculator
{
    int Add(int a, int b);
    
    Task<int> AddAsync(int a, int b);
}
public class Calculator : ICalculator
{
    public int Add(int a, int b)
    {
        //throw new NotImplementedException("This method is not implemented. sorry!");
        return a + b;
    }

    public async Task<int> AddAsync(int a, int b)
    {
        await Task.Delay(1000);
        return a + b;
    }
}
```
示例

```csharp
var decoratedCalculator = LoggingDecorator<ICalculator>.Create(new Calculator());
decoratedCalculator.Add(3, 5);
Console.WriteLine($"Started at {DateTime.Now:HH:mm:ss.fff}");
var res =decoratedCalculator.AddAsync(2, 4);

Console.WriteLine("Waiting for 1 seconds for querying customer...");
Console.WriteLine($"Querying {DateTime.Now:HH:mm:ss.fff}");
Console.WriteLine(res.GetAwaiter().GetResult());
Console.WriteLine($"Finished at {DateTime.Now:HH:mm:ss.fff}");
```
