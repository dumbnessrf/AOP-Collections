public interface ICalculator
{
    int Add(int a, int b);
    
    Task<int> AddAsync(int a, int b);
}