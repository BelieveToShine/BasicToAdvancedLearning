namespace BasicToAdvancedLearning.OOPAdvanced;

// An ABSTRACT class is a half-finished blueprint: it fixes what every transaction has and must do,
// but is too general to exist on its own — `new Transaction(...)` will not compile. Only its
// concrete children can be created.
public abstract class Transaction
{
    private readonly decimal _amount;

    // protected: only the concrete children call this, through `: base(amount)`.
    protected Transaction(decimal amount)
    {
        _amount = amount;
    }

    protected decimal Amount => _amount;

    // `abstract` = no body here, and EVERY concrete child must supply one.
    // It applies the transaction to an account and returns a sentence describing what happened.
    public abstract string Apply(Account account);
}

public class DepositTransaction : Transaction
{
    public DepositTransaction(decimal amount) : base(amount)
    {
    }

    public override string Apply(Account account)
    {
        bool done = account.Deposit(Amount);
        return done ? $"Deposited {Amount}" : $"Deposit of {Amount} refused";
    }
}

public class WithdrawalTransaction : Transaction
{
    public WithdrawalTransaction(decimal amount) : base(amount)
    {
    }

    public override string Apply(Account account)
    {
        bool done = account.Withdraw(Amount);
        return done ? $"Withdrew {Amount}" : $"Withdrawal of {Amount} refused";
    }
}
