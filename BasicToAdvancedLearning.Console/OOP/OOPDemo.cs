namespace BasicToAdvancedLearning.OOP;

// Topic 5: OOP — classes, objects, constructors and encapsulation. One running example
// (a BankAccount, see BankAccount.cs) threads through every method so the ideas connect:
// the class, how it is built, how it protects itself, what a variable really holds, and
// what is shared by all objects (Bank.cs). Each concept gets its own method so it can be
// explained and demoed one at a time from OOPExample.
// See ArchitectureDiagrams/OOP for the visual walkthrough.
public class OOPDemo
{
    // A class is the blueprint; `new` stamps real objects out of it.
    public void ExplainClassesAndObjects()
    {
        PrintHeading("1. Classes & Objects — one blueprint, many independent objects");

        // BankAccount is the CLASS: it lists what every account has and can do.
        // `new` builds a real OBJECT from it — here, two separate ones.
        BankAccount anitaAccount = new BankAccount("Anita", 500m);
        BankAccount raviAccount = new BankAccount("Ravi", 1200m);

        Console.WriteLine(anitaAccount.Describe());
        Console.WriteLine(raviAccount.Describe());

        // Each object keeps its OWN copy of the fields, so changing one never touches the other.
        anitaAccount.Deposit(250m);
        Console.WriteLine($"After Anita deposits 250 -> {anitaAccount.Describe()}");
        Console.WriteLine($"Ravi is unaffected       -> {raviAccount.Describe()}");
    }

    // A constructor sets an object up the moment it is created.
    public void ExplainConstructors()
    {
        PrintHeading("2. Constructors — setting an object up the moment it is born");

        // The two-argument constructor runs automatically at `new` and fills in the fields.
        BankAccount fullAccount = new BankAccount("Anita", 500m);

        // The one-argument constructor is an OVERLOAD: it hands off to the main one with
        // `: this(ownerName, NoMoney)`, so the account starts at zero.
        BankAccount emptyAccount = new BankAccount("Ravi");

        // A constructor can guard the object's first state: -50 is clamped to zero, so
        // no BankAccount can ever begin life with a negative balance.
        BankAccount clampedAccount = new BankAccount("Meena", -50m);

        Console.WriteLine(fullAccount.Describe());
        Console.WriteLine(emptyAccount.Describe());
        Console.WriteLine($"{clampedAccount.Describe()}  (the -50 was clamped)");
    }

    // Private fields + public methods: the object guards its own data.
    public void ExplainEncapsulation()
    {
        PrintHeading("3. Encapsulation — the object guards its own data");

        BankAccount account = new BankAccount("Anita", 500m);
        Console.WriteLine($"Owner, read through a property: {account.Owner}");

        // _balance is private, so outside code cannot write to it. This line would not compile:
        // account._balance = 1_000_000m;   // error: '_balance' is inaccessible due to its protection level
        // The only way in is through methods that enforce the rules:
        bool depositAccepted = account.Deposit(-20m);
        Console.WriteLine($"Deposit of -20 accepted? {depositAccepted} (balance still {account.Balance})");

        bool overdrawAccepted = account.Withdraw(900m);
        Console.WriteLine($"Withdraw of 900 accepted? {overdrawAccepted} (balance still {account.Balance})");

        bool withdrawAccepted = account.Withdraw(200m);
        Console.WriteLine($"Withdraw of 200 accepted? {withdrawAccepted} (balance now {account.Balance})");
    }

    // An object variable holds a pointer, so `=` copies the pointer, not the object.
    public void ExplainReferencesVsCopies()
    {
        PrintHeading("4. References vs Copies — a variable is a key, not the house");

        BankAccount accountA = new BankAccount("Anita", 500m);

        // This copies the KEY (the pointer), not the house (the object): both variables
        // now open the SAME BankAccount. A class is a reference type.
        BankAccount accountB = accountA;
        accountB.Deposit(100m);
        Console.WriteLine($"Deposited 100 via accountB -> accountA sees: {accountA.Describe()}");

        // `new` is the only thing that builds a second, independent object.
        BankAccount accountC = new BankAccount("Anita", 500m);
        // ReferenceEquals asks "are these two variables pointing at the very same object?"
        Console.WriteLine($"Same object? accountA & accountB: {ReferenceEquals(accountA, accountB)}");
        Console.WriteLine($"Same object? accountA & accountC: {ReferenceEquals(accountA, accountC)}");
        Console.WriteLine($"accountC is untouched: {accountC.Describe()}");
    }

    // `static` means one shared copy per type, not one per object.
    public void ExplainStaticMembers()
    {
        PrintHeading("5. Static Members — one copy shared by the whole program");

        // Bank.Name and the counter inside Bank exist ONCE, on the type itself — you reach
        // them through the type name (Bank.), never through an account object.
        Console.WriteLine($"Bank name (static): {Bank.Name}");

        BankAccount anitaAccount = Bank.OpenAccount("Anita", 500m);
        BankAccount raviAccount = Bank.OpenAccount("Ravi", 1200m);

        // Two objects, each with its own _balance — but still just ONE shared counter.
        Console.WriteLine($"{anitaAccount.Describe()} / {raviAccount.Describe()}");
        int openedCount = Bank.AccountsOpened;
        Console.WriteLine($"Accounts opened so far (static counter): {openedCount}");
    }

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
    }
}
