namespace SampleApi.Models;

public class CreateTransactionRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}
