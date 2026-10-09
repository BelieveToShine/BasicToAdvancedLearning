namespace BasicToAdvancedLearning.OOPAdvanced;

// The BASE class for Topic 6. It holds what every kind of account shares; SavingsAccount and
// CurrentAccount inherit all of it and add their own extras. (Topic 5's BankAccount is left
// untouched — this topic builds its own small family so nothing earlier has to change.)
public class Account
{
    // A named zero and a named flat fee, so the rules read as rules rather than bare numbers.
    private const decimal NoMoney = 0m;
    private const decimal StandardFee = 5m;

    private readonly string _ownerName;

    // `protected` = visible inside this class AND inside classes that inherit from it,
    // but hidden from everyone else. Derived classes need the balance; outsiders do not.
    protected decimal _balance;

    public Account(string ownerName, decimal openingBalance)
    {
        _ownerName = ownerName;
        _balance = openingBalance < NoMoney ? NoMoney : openingBalance;
    }

    public string Owner => _ownerName;

    public decimal Balance => _balance;

    // Adds money; refuses (returns false) if the amount is not positive.
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

    // `virtual` = "a derived class MAY replace this with its own version" (see polymorphism).
    public virtual decimal MonthlyFee() => StandardFee;

    public virtual string Describe() => $"{_ownerName} has {_balance}";
}
