namespace RealProxy;

public interface IRepository<T>
{
    void Add(T entity);
    Task<IEnumerable<T>> GetAll();
}