namespace BasicToAdvancedLearning.WebApiBasics;

// The CONTRACT the controller depends on: "something that can list, find and add accounts".
// The controller only knows this interface, never the class behind it (see Topic 6, interfaces).
public interface IAccountStore
{
    IReadOnlyList<Account> GetAll();
    Account? Find(int id);
    Account Add(string accountNumber, string accountType);
}

// One implementation: the accounts sit in a list in memory (no database in this topic).
// The constructor prints a line so the lesson can SHOW when the container creates a store.
public class InMemoryAccountStore : IAccountStore
{
    private readonly List<Account> _accounts = new()
    {
        new Account { AccountId = 1, AccountNumber = "AC-1001", AccountType = "Savings", Balance = 500m },
        new Account { AccountId = 2, AccountNumber = "AC-1002", AccountType = "Current", Balance = 1200m },
    };

    public InMemoryAccountStore()
    {
        Console.WriteLine("    [server] a new InMemoryAccountStore was created");
    }

    public IReadOnlyList<Account> GetAll() => _accounts;

    public Account? Find(int id) => _accounts.FirstOrDefault(a => a.AccountId == id);

    public Account Add(string accountNumber, string accountType)
    {
        var account = new Account
        {
            AccountId = _accounts.Count + 1,   // demo only: a real store/database generates unique ids
            AccountNumber = accountNumber,
            AccountType = accountType,
            Balance = 0m,
        };
        _accounts.Add(account);
        return account;
    }
}
