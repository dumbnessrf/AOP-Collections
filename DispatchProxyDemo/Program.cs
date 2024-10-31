
var decoratedCalculator = LoggingDecorator<ICalculator>.Create(new Calculator());
decoratedCalculator.Add(3, 5);
Console.WriteLine($"Started at {DateTime.Now:HH:mm:ss.fff}");
var res =decoratedCalculator.AddAsync(2, 4);

Console.WriteLine("Waiting for 1 seconds for querying customer...");
Console.WriteLine($"Querying {DateTime.Now:HH:mm:ss.fff}");
Console.WriteLine(res.GetAwaiter().GetResult());
Console.WriteLine($"Finished at {DateTime.Now:HH:mm:ss.fff}");