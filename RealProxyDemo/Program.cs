namespace RealProxy;

public class Program
{
    public static async Task Main(string[] args)
    {
        IRepository<Customer> customerRepository = RepositoryFactory.Create<Customer>();
        var customer = new Customer
        {
            Id = 1,
            Name = "Customer 1",
            Address = "Address 1"
        };
        customerRepository.Add(customer);
        Console.WriteLine($"Started at {DateTime.Now:HH:mm:ss.fff}");
        var allCustomers =  customerRepository.GetAll();
        
        Console.WriteLine("Waiting for 1 seconds for querying customer...");
        Console.WriteLine($"Querying {DateTime.Now:HH:mm:ss.fff}");
        Console.WriteLine("All customers:");
        foreach (var c in allCustomers.GetAwaiter().GetResult())
        {
            Console.WriteLine(c);
        }
        Console.WriteLine($"Finished at {DateTime.Now:HH:mm:ss.fff}");
    }
}
