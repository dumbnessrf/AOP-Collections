namespace RealProxy;

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
