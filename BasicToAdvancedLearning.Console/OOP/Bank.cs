namespace BasicToAdvancedLearning.OOP;

// Static members belong to the TYPE, not to any one object: there is exactly one copy for
// the whole program, no matter how many accounts exist. Only ExplainStaticMembers() uses it.
public static class Bank
{
    // One shared value for the whole program; readonly = assigned once, never changed.
    public static readonly string Name = "KeyBank";

    // One shared counter. Private so only this class can change it (encapsulation again).
    private static int _accountsOpened;

    public static int AccountsOpened => _accountsOpened;

    // A static method: called on the type (Bank.OpenAccount), not on an object.
    // It counts the account, then stamps out a new BankAccount and hands it back.
    public static BankAccount OpenAccount(string ownerName, decimal openingBalance)
    {
        _accountsOpened++;
        return new BankAccount(ownerName, openingBalance);
    }
}
