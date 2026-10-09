namespace BasicToAdvancedLearning.OOP;

// The running example for Topic 5: a bank account. This class is the BLUEPRINT — it says
// what every account has (an owner, a balance) and what every account can do (Deposit,
// Withdraw, Describe). Each `new BankAccount(...)` stamps out one real OBJECT from it.
// Amounts like 500m in OOPDemo are literal on purpose: they are the data the lesson shows.
public class BankAccount
{
    // A named zero, so the rule "money can never go below nothing" reads as a rule, not a bare 0.
    private const decimal NoMoney = 0m;

    // FIELDS hold each object's own data. They are private: only code inside this class
    // can touch them (encapsulation). readonly = set once in a constructor, never again.
    private readonly string _ownerName;
    private decimal _balance;

    // Main constructor: runs automatically at `new` and leaves the object in a valid state.
    // A negative opening balance is clamped to zero (exceptions arrive in Topic 7).
    public BankAccount(string ownerName, decimal openingBalance)
    {
        _ownerName = ownerName;
        // `a ? b : c` is "if a then b, otherwise c" in one line: a negative opening balance becomes NoMoney.
        _balance = openingBalance < NoMoney ? NoMoney : openingBalance;
    }

    // Overloaded constructor: hands the work to the main one via `: this(...)`, so the
    // set-up rules live in exactly one place. An account opened this way starts empty.
    public BankAccount(string ownerName) : this(ownerName, NoMoney)
    {
    }

    // Read-only PROPERTIES: callers can look at the data but cannot assign to it.
    // `=> _ownerName` means "this property simply hands back the field" (Balance below does the same).
    public string Owner => _ownerName;

    public decimal Balance => _balance;

    // Adds money; refuses (returns false) if the amount is not a positive number.
    public bool Deposit(decimal amount)
    {
        if (amount <= NoMoney)
        {
            return false;
        }

        _balance += amount;
        return true;
    }

    // Takes money out; refuses (returns false) if the amount is not positive or exceeds the balance.
    public bool Withdraw(decimal amount)
    {
        if (amount <= NoMoney || amount > _balance)
        {
            return false;
        }

        _balance -= amount;
        return true;
    }

    // A one-line summary of THIS object's data, used by the lesson's Console.WriteLine calls.
    public string Describe() => $"{_ownerName} has {_balance} in the account";
}
