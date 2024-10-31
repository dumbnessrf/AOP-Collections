using ContextBoundObjectDemo;

public abstract class WorkerAttribute : Attribute
{
    public abstract IDualWorkerService GetWorker();

    public abstract void DoWorkBefore();
    public abstract void DoWorkAfter();
}