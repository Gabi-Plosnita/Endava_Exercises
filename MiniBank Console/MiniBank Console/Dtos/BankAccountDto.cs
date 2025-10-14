using MiniBank_Console.Models;

namespace MiniBank_Console.Dtos;

public class BankAccountDto
{
    public AccountType Type { get; set; }
    public int Id { get; set; }
    public string Owner { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public List<string> OperationLog { get; set; } = [];
    public DateTime EndDate { get; set; }
}
