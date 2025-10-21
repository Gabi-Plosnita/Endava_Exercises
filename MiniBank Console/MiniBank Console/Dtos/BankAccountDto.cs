using MiniBank_Console.Models;

namespace MiniBank_Console.Dtos;

public record BankAccountDto
{
    public AccountType AccountType { get; set; }
    public int Id { get; set; }
    public string Owner { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public List<string> OperationLog { get; set; } = [];
    public DateTime? EndDate { get; set; }
}
