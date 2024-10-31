public class Calculator : ICalculator
{
    public int Add(int a, int b)
    {
        //throw new NotImplementedException("This method is not implemented. sorry!");
        return a + b;
    }

    public async Task<int> AddAsync(int a, int b)
    {
        await Task.Delay(1000);
        return a + b;
    }
}