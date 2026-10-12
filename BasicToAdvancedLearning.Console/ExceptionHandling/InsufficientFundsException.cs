namespace BasicToAdvancedLearning.ExceptionHandling;

// A CUSTOM exception: a class of our own that inherits from Exception (Topic 6's inheritance at work).
// Giving a problem its own type lets callers catch exactly this case, and the type can carry data
// that explains what went wrong — here, how much money was missing.
public class InsufficientFundsException : Exception
{
    private readonly decimal _shortfall;

    // `: base(...)` hands the human-readable message to Exception, which stores it in Message.
    public InsufficientFundsException(decimal shortfall)
        : base($"Not enough money: short by {shortfall}")
    {
        _shortfall = shortfall;
    }

    // The extra data this exception carries to whoever catches it.
    public decimal Shortfall => _shortfall;
}
