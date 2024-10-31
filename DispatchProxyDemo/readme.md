# DispatchProxy 
`NET Core`
DispatchProxy ��һ���� .NET ���������ĸ���ر����� C# �����С�����һ���������͵Ĵ�������������ʱ��̬�ط��ɷ������õ���ͬ��ʵ�֡�DispatchProxy ��λ�� System.Runtime.Remoting.Proxies �����ռ��У������� RealProxy ���һ�������ࡣ

������ DispatchProxy ��һЩ�ؼ��㣺

��̬���ɣ�DispatchProxy ��������������ʱ�������������÷��ɸ��ĸ�������ʹ�ÿ����ڲ��޸����д��������£���̬�ظı�������Ϊ��

͸������DispatchProxy ��һ��͸����������ζ�ſͻ��˴��벻��Ҫ֪������Ĵ��ڡ��ͻ��˴������ֱ�ӵ��÷�������������ֱ�ӵ���ʵ�ʶ���һ����

���Ͱ�ȫ������ DispatchProxy �Ƕ�̬���ɵģ�������Ȼ�������Ͱ�ȫ�����������Ա�ǿ��ת��Ϊ�κνӿ����ͣ�����ֻ����Щ�ӿ��ϵķ������òŻᱻ���ɡ�

�����Զ�����������߿���ͨ���̳� DispatchProxy �ಢ��д Invoke �����������Լ��Ĵ����� Invoke �����У������߿��Ծ�����η��ɷ������ã����磬���ڵ��õķ������������������߼���

���ܿ��������� DispatchProxy �漰������ʱ�Ķ�̬���ɣ���˿��ܻ���һЩ���ܿ�������ˣ������ʺ�����Щ���ܲ�����Ҫ��ע��ĳ�����

��;��DispatchProxy �������ڶ��ֳ�������������������־��¼�����ܼ�ء����������ȫ�Լ�顢Mock����Ĵ����ȡ�


**�����RealProxy��ContextBoundObject**��DispatchProxy��ʵ�ָ��ӱ��
## ������־������

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
                        }, /*�ɹ�ʱִ��*/
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
                        }, /*�ɹ�ʱִ��*/
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
## �����첽�İ�����

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
## ����������ʾ��

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
ʾ��

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
