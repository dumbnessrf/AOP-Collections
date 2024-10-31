using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using ContextBoundObjectDemo;


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