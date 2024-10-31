namespace RealProxy;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }

    public override string ToString()
    {
        return string.Format("Id: {0}, Name: {1}, Address: {2}", Id, Name, Address);
    }
}