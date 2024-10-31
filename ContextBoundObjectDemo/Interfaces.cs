namespace ContextBoundObjectDemo;



public interface IDualWorkerService : IBeforeWorkerService, IAfterWorkerService { }

public interface IBeforeWorkerService
{
    void Before(object obj);
}

public interface IAfterWorkerService
{
    void After(object obj);
}
