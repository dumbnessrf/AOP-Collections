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

    //异步处理方法（不需要）
    public IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
    {
        return null;
    }

    private IMessage BeforeAndAfterDoWork(IMessage msg, IMethodCallMessage methodCall)
    {
        IMessage retMsg;

        var attrs = Attribute.GetCustomAttributes(methodCall.MethodBase);

        

        return WorkersManager.DoWork(attrs, nextSink, msg);
    }
}