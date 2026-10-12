namespace BasicToAdvancedLearning.WebApiCrud;

// The three tables of the running example ("the bank"), as C# classes. EF Core maps each class to a
// table and each property to a column. These are ENTITIES: they mirror the database, so they are
// NOT what the API sends over the wire (the DTOs in AccountDtos.cs do that — see ExplainReadEndpoints).
public class Customer
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    // Navigation property: the accounts that belong to this customer.
    public List<Account> Accounts { get; set; } = new();
}

public class Account
{
    public int AccountId { get; set; }
    public int CustomerId { get; set; }
    public string AccountNumber { get; set; } = "";
    public string AccountType { get; set; } = "";
    public decimal Balance { get; set; }

    // Navigation properties: the owner, and the account's history.
    public Customer? Customer { get; set; }
    public List<BankTransaction> Transactions { get; set; } = new();
}

// Named BankTransaction (not Transaction) so it cannot be confused with a database transaction; it is
// stored in the Transactions table. It exists here so that an account WITH history can be refused a
// delete (the foreign-key conflict in ExplainUpdateAndDelete).
public class BankTransaction
{
    public long TransactionId { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Kind { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
