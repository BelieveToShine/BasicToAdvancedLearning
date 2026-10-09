namespace BasicToAdvancedLearning.OOPAdvanced;

// A second kind of Account. It inherits the same base, adds an overdraft limit,
// and charges a bigger monthly fee than the standard one.
public class CurrentAccount : Account
{
    private const decimal CurrentAccountFee = 10m;

    // Added by CurrentAccount on top of everything it inherits from Account.
    private readonly decimal _overdraftLimit;

    public CurrentAccount(string ownerName, decimal openingBalance, decimal overdraftLimit)
        : base(ownerName, openingBalance)
    {
        _overdraftLimit = overdraftLimit;
    }

    public decimal OverdraftLimit => _overdraftLimit;

    public override decimal MonthlyFee() => CurrentAccountFee;

    public override string Describe() => $"{base.Describe()} (current, overdraft limit {_overdraftLimit})";
}
