using System.Runtime.Remoting.Messaging;
using ContextBoundObjectDemo;


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