namespace BasicToAdvancedLearning.OOPAdvanced;

// Topic 6: OOP advanced — inheritance, polymorphism, abstract classes and interfaces. The bank
// thread from Topic 5 continues with a small family: Account (base), SavingsAccount and
// CurrentAccount (children), Transaction (abstract), and IInterestBearing (an interface that
// SavingsAccount and the unrelated FixedDeposit both sign). Each concept gets its own method so it
// can be explained and demoed one at a time from OOPAdvancedExample.
// See ArchitectureDiagrams/OOPAdvanced for the visual walkthrough.
public class OOPAdvancedDemo
{
    // A child class reuses everything its parent has and adds its own extras.
    public void ExplainInheritance()
    {
        PrintHeading("1. Inheritance — a child reuses everything its parent has");

        // SavingsAccount is declared as `: Account`, so it already has an owner, a balance and
        // Deposit() without repeating any of that code. It only adds an interest rate.
        SavingsAccount savings = new SavingsAccount("Anita", 1000m, 6m);
        CurrentAccount current = new CurrentAccount("Ravi", 300m, 200m);

        savings.Deposit(500m); // inherited from Account — SavingsAccount never wrote it
        Console.WriteLine($"{savings.Owner}'s savings balance (inherited): {savings.Balance}");
        Console.WriteLine($"{savings.Owner}'s interest rate (added by the child): {savings.InterestRatePercent}%");
        Console.WriteLine($"{current.Owner}'s overdraft limit (added by the child): {current.OverdraftLimit}");
    }

    // One call, many behaviours: the real object decides which version of a virtual method runs.
    public void ExplainPolymorphism()
    {
        PrintHeading("2. Polymorphism — one call, the real object decides");

        // The array's element type is Account, yet it holds a savings, a current and a plain account:
        // a child object can always stand in for its parent.
        Account[] accounts =
        {
            new SavingsAccount("Anita", 1000m, 6m),
            new CurrentAccount("Ravi", 300m, 200m),
            new Account("Meena", 50m)
        };

        decimal totalFees = 0m;
        foreach (Account account in accounts)
        {
            // We only ever call the Account methods, but each object runs ITS OWN override.
            Console.WriteLine($"{account.Describe()} -> monthly fee {account.MonthlyFee()}");
            totalFees += account.MonthlyFee();
        }

        Console.WriteLine($"Total monthly fees: {totalFees}");
    }

    // An abstract class fixes WHAT must be done and leaves HOW to each child.
    public void ExplainAbstractClasses()
    {
        PrintHeading("3. Abstract Classes — a template that cannot be built on its own");

        Account account = new Account("Anita", 500m);

        // Transaction is abstract, so this line would not compile:
        // Transaction broken = new Transaction(100m);   // error: cannot create an instance of the abstract type
        // Only the concrete children can be created, and each MUST implement Apply().
        Transaction[] batch =
        {
            new DepositTransaction(200m),
            new WithdrawalTransaction(900m),
            new WithdrawalTransaction(100m)
        };

        foreach (Transaction transaction in batch)
        {
            Console.WriteLine($"{transaction.Apply(account)} -> balance {account.Balance}");
        }
    }

    // An interface is a contract any class can sign, even classes from different families.
    public void ExplainInterfaces()
    {
        PrintHeading("4. Interfaces — a contract any class can sign");

        // SavingsAccount (an Account) and FixedDeposit (NOT an Account) both sign IInterestBearing,
        // so one array of the interface type can hold both. A class can sign many interfaces
        // but inherit from only one base class.
        IInterestBearing[] earners =
        {
            new SavingsAccount("Anita", 1000m, 6m),
            new FixedDeposit(6000m, 8m)
        };

        decimal totalInterest = 0m;
        foreach (IInterestBearing earner in earners)
        {
            decimal interest = earner.MonthlyInterest();
            Console.WriteLine($"{earner.GetType().Name} earns {interest} this month");
            totalInterest += interest;
        }

        Console.WriteLine($"Total interest: {totalInterest}");
    }

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
    }
}
