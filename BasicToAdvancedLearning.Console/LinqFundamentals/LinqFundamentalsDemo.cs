namespace BasicToAdvancedLearning.LinqFundamentals;

// Topic 9: LINQ fundamentals — asking questions of a collection (Topic 4) in one readable line
// instead of writing the loop yourself. The running example continues the bank thread: a List<decimal> of
// amounts where a positive number is a deposit and a negative number is a withdrawal.
// `x => ...` is a LAMBDA: a tiny nameless method, read as "each x, such that ...". The methods
// (Where, Select, OrderBy, Sum, ...) come from System.Linq, which the project already imports.
// Each concept gets its own method so it can be explained and demoed one at a time from
// LinqFundamentalsExample. See ArchitectureDiagrams/LinqFundamentals for the visual walkthrough.
public class LinqFundamentalsDemo
{
    // Named values, so the rules in the lambdas read as rules rather than bare numbers.
    private const decimal NoMoney = 0m;
    private const decimal LargeAmount = 1000m;
    private const decimal HugeAmount = 5000m;

    // Where keeps the items a lambda approves; Select turns every item into something else.
    public void ExplainWhereAndSelect()
    {
        PrintHeading("1. Where and Select — filter, then transform");

        List<decimal> amounts = CreateAmounts();

        // Where keeps only the items for which the lambda returns true. ToList() runs the query and
        // collects the survivors into a brand-new List (the original list is never changed).
        List<decimal> deposits = amounts.Where(amount => amount > NoMoney).ToList();

        // Select turns every item into something else — here each decimal becomes a short text label.
        List<string> labels = deposits.Select(amount => $"+{amount}").ToList();

        Console.WriteLine($"All amounts:           {string.Join(", ", amounts)}");
        Console.WriteLine($"Deposits only (Where): {string.Join(", ", deposits)}");
        Console.WriteLine($"Labels (Select):       {string.Join(", ", labels)}");
    }

    // OrderBy sorts into a new sequence; Sum, Max and Count boil many items down to one answer.
    public void ExplainOrderingAndAggregates()
    {
        PrintHeading("2. Ordering and aggregates — sort it, then boil it down");

        List<decimal> amounts = CreateAmounts();

        // OrderBy(...) only PLANS a sorted sequence (the source list keeps its own order);
        // ToList() runs that plan and builds the new sorted list `ordered`.
        List<decimal> ordered = amounts.OrderBy(amount => amount).ToList();

        decimal total = amounts.Sum();
        decimal largest = amounts.Max(); // on an EMPTY list Max() throws InvalidOperationException (Sum() would give 0)
        int withdrawalCount = amounts.Count(amount => amount < NoMoney); // Count can take a lambda too

        Console.WriteLine($"Sorted (OrderBy):      {string.Join(", ", ordered)}");
        Console.WriteLine($"Original is untouched: {string.Join(", ", amounts)}");
        Console.WriteLine($"Sum {total}, largest {largest}, withdrawals {withdrawalCount}");
    }

    // Any, All and First answer questions about a collection instead of returning a new one.
    public void ExplainAskingQuestions()
    {
        PrintHeading("3. Asking questions — Any, All, First");

        List<decimal> amounts = CreateAmounts();

        bool anyWithdrawal = amounts.Any(amount => amount < NoMoney);  // is at least one item a match?
        bool allDeposits = amounts.All(amount => amount > NoMoney);    // do EVERY item match?

        // First returns the first match. If nothing matched it would THROW an InvalidOperationException
        // (Topic 7); FirstOrDefault returns the type's default instead — 0 for a decimal.
        decimal firstLarge = amounts.First(amount => amount > LargeAmount);
        decimal noMatch = amounts.FirstOrDefault(amount => amount > HugeAmount);

        Console.WriteLine($"Any withdrawal? {anyWithdrawal}.  All deposits? {allDeposits}");
        Console.WriteLine($"First amount over {LargeAmount}: {firstLarge}");
        Console.WriteLine($"Nothing over {HugeAmount}, so FirstOrDefault gives the default: {noMatch}");
    }

    // A Where query is only a PLAN until something enumerates it; ToList() cooks it into a copy.
    public void ExplainDeferredExecution()
    {
        PrintHeading("4. Deferred execution — a query is a plan, not an answer");

        List<decimal> amounts = new List<decimal> { 200m, -900m, 50m };

        // This line filters NOTHING yet. `deposits` just remembers the list and the lambda.
        IEnumerable<decimal> deposits = amounts.Where(amount => amount > NoMoney);

        amounts.Add(1200m); // the source changes AFTER the query was written...

        // ...and ToList() is where the plan finally runs, so it sees the new 1200. The result is a
        // separate copy: it will not change again.
        List<decimal> snapshot = deposits.ToList();

        amounts.Add(75m);

        // Count() enumerates `deposits` AGAIN, so it re-reads the source and finds the 75 as well.
        int liveCount = deposits.Count();
        int snapshotCount = snapshot.Count;

        Console.WriteLine($"snapshot (taken before 75 was added): {snapshotCount} deposits");
        Console.WriteLine($"deposits (re-run just now):           {liveCount} deposits");
    }

    // The lesson's data, rebuilt fresh in each method: positive = deposit, negative = withdrawal.
    private static List<decimal> CreateAmounts()
    {
        return new List<decimal> { 200m, -900m, 50m, 1200m, -75m };
    }

    // Prints a three-line banner (rule, title, rule) so each lesson section is easy to spot in the output.
    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
    }
}
