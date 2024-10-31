# ContextBoundObject

**NET Framework需要实现AOP，可以借助于`System.Runtime.Remoting.Contexts`命名空间中的`ContextBoundObject`类。**

`ContextBoundObject` 是 .NET Framework 中的一个抽象类，它继承自 `MarshalByRefObject`，用于定义所有上下文绑定类的基类。上下文绑定对象是指那些驻留在特定上下文中并受上下文规则约束的对象。上下文是一组属性或使用规则，用于定义对象集合所在的环境，当对象进入或离开上下文时，会强制实施这些规则。

上下文绑定的对象只能在创建它的上下文中正常运行，而其他对象访问它时，必须通过透明代理（Transparent Proxy）来操作。这与上下文灵活对象（context-agile）不同，后者可以存在于任意上下文中，并且不需要特定的操作就可以被创建和管理。

`ContextBoundObject` 类的一个重要用途是实现自动同步。通过将 `SynchronizationAttribute` 应用于 `ContextBoundObject` 的子类，可以确保该类的实例在同一时刻只能被一个线程访问，从而实现线程安全。这种机制是通过在每个方法或属性的每次调用时自动加锁来实现的，锁的作用域被称为同步上下文。

然而，需要注意的是，当前版本的公共语言运行时（CLR）不支持具有泛型方法的泛型 `ContextBoundObject` 类型或非泛型 `ContextBoundObject` 类型。尝试创建此类的实例会导致 `TypeLoadException` 异常。

在实际应用中，`ContextBoundObject` 可以用来实现诸如事务处理、同步访问等跨多个对象的操作，同时保持代码的简洁性和易于管理。不过，由于 .NET 的发展，一些过去使用 `ContextBoundObject` 实现的功能可能已经有了更现代的替代方案。
## 适用场景
`NET Framework`
## 使用步骤
引用 `System.Runtime.Remoting` 命名空间。
定义一个要进行同步上下文的类，并继承自 `ContextBoundObject` 类。
```csharp
class Data : ContextBoundObject
{
    public void DoWork()
    {
        Console.WriteLine("正在处理。。。。。");
    }
}
```
`AopHandler.cs`:定义在同步上下文时执行的逻辑
```csharp
using System.Runtime.Remoting.Messaging;

public sealed class AopHandler : IMessageSink
{
    //下一个接收器
    private IMessageSink nextSink;
    public IMessageSink NextSink
    {
        get { return nextSink; }
    }

    public AopHandler(IMessageSink nextSink)
    {
        this.nextSink = nextSink;
    }

    //同步处理方法
    public IMessage SyncProcessMessage(IMessage msg)
    {
        IMessage retMsg = null;

        //方法调用消息接口
        IMethodCallMessage call = msg as IMethodCallMessage;

        if (call == null)
        {
            retMsg = nextSink.SyncProcessMessage(msg);
            return retMsg;
        }
            
        retMsg = BeforeAndAfterDoWork(msg, call);

        return retMsg;
    }

    public IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
    {
        return null;
    }

    public IMessage BeforeAndAfterDoWork(IMessage msg, IMethodCallMessage methodCall)
    {
        IMessage retMsg;

        var attrs = Attribute.GetCustomAttributes(methodCall.MethodBase);

        return WorkersManager.DoWork(attrs, nextSink, msg);
    }
}
```
`WorkersManager.cs`:定义执行逻辑管理类
```csharp
public class WorkersManager
{

    public static IMessage DoWork(
        Attribute[] attributes,
        IMessageSink messageSink,
        IMessage message
    )
    {
        //循环遍历所有属性，如果是WorkerAttribute的子类，则执行DoWorkBefore和DoWorkAfter方法
        foreach (var item in attributes)
        {
            if (item.GetType().IsSubclassOf(typeof(WorkerAttribute)))
            {
                (item as WorkerAttribute).DoWorkBefore();
            }
        }
        var ret = messageSink.SyncProcessMessage(message);
        foreach (var item in attributes)
        {
            if (item.GetType().IsSubclassOf(typeof(WorkerAttribute)))
            {
                (item as WorkerAttribute).DoWorkAfter();
            }
        }
        return ret;
    }
}
```
`InterceptorAttribute.cs`:定义拦截器属性，标记了该特性的类
在执行时拦截
```csharp
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class InterceptorAttribute : ContextAttribute, IContributeObjectSink
{
    public InterceptorAttribute()
        : base("Interceptor") { }

    //实现IContributeObjectSink接口当中的消息接收器接口
    public IMessageSink GetObjectSink(MarshalByRefObject obj, IMessageSink next)
    {
        return new AopHandler(next);
    }
}

public abstract class WorkerAttribute : Attribute
{
    public abstract IDualWorkerService GetWorker();

    public abstract void DoWorkBefore();
    public abstract void DoWorkAfter();
}

public interface IDualWorkerService : IBeforeWorkerService, IAfterWorkerService { }

public interface IBeforeWorkerService
{
    void Before(object obj);
}

public interface IAfterWorkerService
{
    void After(object obj);
}
```
记录方法进入和退出日志
```csharp
public class LoggingWorker : IDualWorkerService
{
    public void Before(object obj)
    {
        Console.WriteLine(
            $"{DateTime.Now.ToString("HH:mm:ss.fff")} {obj as string} Before"
        );
    }

    public void After(object obj)
    {
        Console.WriteLine(
            $"{DateTime.Now.ToString("HH:mm:ss.fff")} {obj as string} After"
        );
    }
}
```
使用案例
```csharp
Console.WriteLine("Hello, World!");

Data data = new Data();
data.DoWork();

Console.ReadKey();


[Interceptor]
class Data : ContextBoundObject
{
    [LoggingInterceptor("点击了按钮。。。。")]
    public void DoWork()
    {
        Console.WriteLine("正在处理。。。。。");
        throw new Exception("出错了！");
    }
}

```
