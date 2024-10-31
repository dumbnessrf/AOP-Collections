using ContextBoundObjectDemo;

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