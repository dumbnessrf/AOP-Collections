# RealProxy
`NET Framework`

`RealProxy` 是 .NET Framework 中的一个类，它位于 `System.Runtime.Remoting.Proxies` 命名空间中。它是所有代理类必须继承的抽象基类。`RealProxy` 类的主要功能是在运行时动态生成一个透明代理（`TransparentProxy`），这个透明代理与它所代理的实际对象具有相同的类型信息，使得客户端代码在大多数情况下无法区分自己操作的是代理对象还是实际对象。

透明代理提供了一种机制，使得客户端代码可以跨应用程序域甚至跨计算机远程调用对象的方法，而无需关心远程调用的复杂性。`RealProxy` 类通过实现 `IMessage` 接口的 `Invoke` 方法来转发对代理对象的方法调用。当代理对象上的方法被调用时，调用会被封装成一个消息，然后由 `RealProxy` 类的 `Invoke` 方法处理，最终通过远程通信基础设施发送到实际对象所在的远程位置。

`RealProxy` 类的关键特点包括：
1. **透明代理生成**：`RealProxy` 类使用 `Type` 类型信息来生成一个透明代理，这个代理对象与被代理对象具有相同的接口或基类。
2. **方法调用转发**：`RealProxy` 类的 `Invoke` 方法会拦截对代理对象的所有方法调用，并将这些调用转发到实际对象。
3. **扩展性**：通过继承 `RealProxy` 类并重写 `Invoke` 方法，开发者可以插入自己的逻辑，例如日志记录、权限检查、事务管理等，来增强或修改代理对象的行为。
4. **安全性**：`RealProxy` 类在类级别上进行了链接需求和继承需求的安全检查，确保只有具有适当权限的代码才能创建或继承代理类。

在使用 `RealProxy` 类时，开发者通常需要执行以下步骤：
- 创建一个继承自 `RealProxy` 的子类。
- 在子类中实现 `Invoke` 方法，以处理对代理对象的方法调用。
- 使用 `RemotingServices.Marshal` 方法将实际对象 marshal 到远程对象引用（`ObjRef`）。
- 通过 `GetTransparentProxy` 方法获取透明代理实例，并将其传递给客户端代码。

`RealProxy` 类是 .NET 远程处理框架的核心组件，它为开发者提供了一种灵活且强大的方式来实现代理模式，从而在不修改实际对象代码的情况下，控制和扩展对象的行为。


## 使用
创建代理类，定义代理逻辑
```csharp
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
```
## 定义要代理的类
```csharp
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }

    public override string ToString()
    {
        return string.Format("Id: {0}, Name: {1}, Address: {2}", Id, Name, Address);
    }
}
```
## 定义辅助类,用于执行异步代理
```csharp
/// <summary>
/// 提供异步方法的帮助程序。
/// </summary>
internal static class InternalAsyncHelper
{
    /// <summary>
    /// 判断指定方法是否是异步方法。
    /// </summary>
    /// <param name="method">需要检查的 <see cref="MethodInfo"/> 对象。</param>
    /// <returns>如果是异步方法，则返回 true；否则返回 false。</returns>
    public static bool IsAsyncMethod(this MethodInfo method)
    {
        return method.ReturnType == typeof(Task)
               || method.ReturnType.IsGenericType
               && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
    }

    /// <summary>
    /// 等待给定的任务并在完成后执行后续操作，最后执行最终操作。
    /// </summary>
    /// <param name="actualReturnValue">实际返回的任务。</param>
    /// <param name="postAction">完成后的操作。</param>
    /// <param name="finalAction">最终操作，包含异常信息（如果有）。</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// 等待给定的任务并在完成后执行后续操作，返回结果并执行最终操作。
    /// </summary>
    /// <typeparam name="T">任务返回值的类型。</typeparam>
    /// <param name="actualReturnValue">实际返回的任务。</param>
    /// <param name="postAction">完成后的操作，接受任务返回的结果。</param>
    /// <param name="finalAction">最终操作，包含异常信息（如果有）。</param>
    /// <returns>任务的结果。</returns>
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

    /// <summary>
    /// 调用指定类型的任务，并在完成后执行后续操作，最终操作返回结果。
    /// </summary>
    /// <param name="taskReturnType">任务返回值的类型。</param>
    /// <param name="actualReturnValue">实际返回的任务。</param>
    /// <param name="action">执行后续操作。</param>
    /// <param name="finalAction">最终操作，包含异常信息（如果有）。</param>
    /// <returns>任务的结果。</returns>
    public static object CallAwaitTaskWithPostActionAndFinallyAndGetResult(
        Type taskReturnType,
        object actualReturnValue,
        Func<object, Task> action,
        Action<Exception> finalAction
    )
    {
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
定义仓储类
```csharp
public interface IRepository<T>
{
    void Add(T entity);
    Task<IEnumerable<T>> GetAll();
}

public class Repository<T> : IRepository<T>
{
    List<T> _entities = new List<T>();

    public void Add(T entity)
    {
        _entities.Add(entity);
        Console.WriteLine($"Adding {{ {entity} }}");
    }

    public async Task<IEnumerable<T>> GetAll()
    {
        await Task.Delay(1000);
// throw new NotImplementedException("Not implemented yet, sorry!");
        return _entities;
    }
}
```
定义工厂类
```csharp
public class RepositoryFactory
{
    public static IRepository<T> Create<T>()
    {
        var repository = new Repository<T>();
        var dynamicProxy = new DynamicProxy<IRepository<T>>(repository);
        return dynamicProxy.GetTransparentProxy() as IRepository<T>;
    }
}
```
使用案例
```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        IRepository<Customer> customerRepository = RepositoryFactory.Create<Customer>();
        var customer = new Customer
        {
            Id = 1,
            Name = "Customer 1",
            Address = "Address 1"
        };
        customerRepository.Add(customer);
        Console.WriteLine($"Started at {DateTime.Now:HH:mm:ss.fff}");
        var allCustomers =  customerRepository.GetAll();
        
        Console.WriteLine("Waiting for 1 seconds for querying customer...");
        Console.WriteLine($"Querying {DateTime.Now:HH:mm:ss.fff}");
        Console.WriteLine("All customers:");
        foreach (var c in allCustomers.GetAwaiter().GetResult())
        {
            Console.WriteLine(c);
        }
        Console.WriteLine($"Finished at {DateTime.Now:HH:mm:ss.fff}");
    }
}
```