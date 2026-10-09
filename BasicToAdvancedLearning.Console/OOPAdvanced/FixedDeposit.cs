namespace BasicToAdvancedLearning.OOPAdvanced;

// NOT an Account — it has no `: Account` — yet it can still sign the IInterestBearing contract.
// That is the point of an interface: unrelated classes become interchangeable for one ability.
public class FixedDeposit : IInterestBearing
{
    private const decimal PercentDivisor = 100m;
    private const decimal MonthsInYear = 12m;

    private readonly decimal _principal;
    private readonly decimal _annualRatePercent;

    public FixedDeposit(decimal principal, decimal annualRatePercent)
    {
        _principal = principal;
        _annualRatePercent = annualRatePercent;
    }

    public decimal MonthlyInterest() => _principal * _annualRatePercent / PercentDivisor / MonthsInYear;
}
