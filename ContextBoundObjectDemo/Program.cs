using ContextBoundObjectDemo;

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
