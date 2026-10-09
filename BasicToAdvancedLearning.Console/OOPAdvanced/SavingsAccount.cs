namespace BasicToAdvancedLearning.OOPAdvanced;

// A SavingsAccount IS-AN Account (the `: Account` part) and also IS-AN IInterestBearing.
// A class may inherit from only ONE base class, but it may implement many interfaces.
public class SavingsAccount : Account, IInterestBearing
{
    private const decimal NoFee = 0m;
    private const decimal PercentDivisor = 100m;
    private const decimal MonthsInYear = 12m;

    // Added by SavingsAccount on top of everything it inherits from Account.
    private readonly decimal _interestRatePercent;

    // `: base(...)` runs Account's constructor first, so the inherited fields are set up
    // before this class adds its own.
    public SavingsAccount(string ownerName, decimal openingBalance, decimal interestRatePercent)
        : base(ownerName, openingBalance)
    {
        _interestRatePercent = interestRatePercent;
    }

    public decimal InterestRatePercent => _interestRatePercent;

    // `override` replaces Account's version: savings accounts charge no monthly fee.
    public override decimal MonthlyFee() => NoFee;

    // Reuses the base version with `base.Describe()` and adds what is specific to savings.
    public override string Describe() => $"{base.Describe()} (savings, {_interestRatePercent}% interest)";

    // The one method the IInterestBearing contract demands.
    public decimal MonthlyInterest() => _balance * _interestRatePercent / PercentDivisor / MonthsInYear;
}
