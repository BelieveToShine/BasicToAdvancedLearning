using System.ComponentModel.DataAnnotations;

namespace FullStackBank.Api.Data;

// Entities are the classes EF Core maps to database tables. They are INTERNAL to the API: controllers never
// return them directly (they return DTOs, see Dtos/), so a table change cannot silently change the JSON
// contract, and fields the client must not see or set never leak.

public enum AccountType
{
    Savings,
    Current,
}

public enum TransactionKind
{
    Deposit,
    Withdrawal,
}

public class Customer
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public List<Account> Accounts { get; set; } = new();
}

public class Account
{
    public int AccountId { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string AccountNumber { get; set; } = "";
    public AccountType AccountType { get; set; }
    public decimal Balance { get; set; }

    // SQL Server changes this value on every UPDATE of the row. EF adds "WHERE RowVersion = <the value I read>"
    // to its UPDATE, so if two requests change the same account at the same time, the second one is detected
    // (0 rows updated) instead of silently overwriting the first. See ApiExceptionHandler for the 409 mapping.
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<BankTransaction> Transactions { get; set; } = new();
}

// Named BankTransaction (not Transaction) so it is never confused with a database transaction.
public class BankTransaction
{
    public long TransactionId { get; set; }
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public TransactionKind Kind { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
