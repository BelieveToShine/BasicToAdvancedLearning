namespace BasicToAdvancedLearning.ExceptionHandling;

// Topic 7's running example: an account whose Withdraw() THROWS when something is wrong, instead of
// quietly returning false like Topic 5's BankAccount did. Exceptions suit problems the method cannot
// fix itself — the caller decides what to do.
public class SafeAccount
{
    private const decimal NoMoney = 0m;

    private readonly string _ownerName;
    private decimal _balance;

    public SafeAccount(string ownerName, decimal openingBalance)
    {
        _ownerName = ownerName;
        _balance = openingBalance;
    }

    public string Owner => _ownerName;

    public decimal Balance => _balance;

    // Takes money out, or throws: a built-in exception for a nonsense amount, and our own custom
    // exception when the account cannot cover it. `throw` stops this method on the spot.
    public void Withdraw(decimal amount)
    {
        if (amount <= NoMoney)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal must be a positive amount.");
        }

        if (amount > _balance)
        {
            throw new InsufficientFundsException(amount - _balance);
        }

        _balance -= amount;
    }
}
