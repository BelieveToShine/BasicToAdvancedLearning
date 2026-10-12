namespace BasicToAdvancedLearning.EfCoreCrud;

// The three "entity" classes of the bank. An entity is a plain C# class that EF Core maps to one table:
// each property is a column, each object is a row. Nothing here knows about SQL Server.
// Topic 15 builds its own model so it does not depend on any other topic's code.

// One row of the Customers table.
public class Customer
{
    public int CustomerId { get; set; }                    // primary key (SQL Server makes the number)
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }                // filled in by the database (a default)

    // "Navigation property": the customer's accounts. It is EMPTY until accounts are loaded or added.
    public List<Account> Accounts { get; set; } = new();
}

// One row of the Accounts table.
public class Account
{
    public int AccountId { get; set; }
    public int CustomerId { get; set; }                    // foreign key column
    public string AccountNumber { get; set; } = "";
    public string AccountType { get; set; } = "";          // "Savings" or "Current"
    public decimal Balance { get; set; }

    public Customer? Customer { get; set; }                // navigation back to the owner
    public List<BankTransaction> Transactions { get; set; } = new();
}

// One row of the Transactions table. The class is called BankTransaction so it is not confused with
// System.Transactions; the TABLE is still named Transactions (see BankContext).
public class BankTransaction
{
    public long TransactionId { get; set; }               // BIGINT in the database
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Kind { get; set; } = "";                 // "Deposit" or "Withdrawal"
    public DateTime CreatedAt { get; set; }

    public Account? Account { get; set; }
}

// Named values for the text columns, so the strings are not repeated through the lesson.
public static class AccountTypes
{
    public const string Savings = "Savings";
    public const string Current = "Current";
}

public static class TransactionKinds
{
    public const string Deposit = "Deposit";
    public const string Withdrawal = "Withdrawal";
}
