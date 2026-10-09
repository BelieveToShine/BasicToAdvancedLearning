namespace BasicToAdvancedLearning.OOPAdvanced;

// An INTERFACE is a pure contract: a list of what a type can DO, with no fields and no
// ready-made code. Any class can sign it, whatever family it belongs to.
public interface IInterestBearing
{
    // Interest earned in one month.
    decimal MonthlyInterest();
}
