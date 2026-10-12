namespace BasicToAdvancedLearning.ExceptionHandling;

// Topic 7: Exception handling — try/catch, catch order, finally, and custom exceptions. The bank
// thread continues with SafeAccount, whose Withdraw() throws instead of returning false (Topic 5's
// BankAccount said "exceptions arrive in Topic 7" — this is that topic). Each concept gets its own
// method so it can be explained and demoed one at a time from ExceptionHandlingExample.
// See ArchitectureDiagrams/ExceptionHandling for the visual walkthrough.
public class ExceptionHandlingDemo
{
    // try runs risky code; catch is the safety net that stops a crash.
    public void ExplainTryCatch()
    {
        PrintHeading("1. try / catch — a safety net under risky code");

        string typedAmount = "abc"; // pretend a user typed this instead of a number

        try
        {
            // decimal.Parse THROWS a FormatException for text that is not a number.
            // (Topic 1's TryParse avoided this by returning false instead — exceptions are for surprises.)
            decimal amount = decimal.Parse(typedAmount);
            Console.WriteLine($"Parsed {amount}"); // never runs: the line above already threw
        }
        catch (FormatException ex)
        {
            // `ex` points at the exception object describing what went wrong.
            Console.WriteLine($"Caught a {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine("The program carried on instead of crashing.");
    }

    // Several catch clauses are checked top to bottom; the FIRST that fits wins.
    public void ExplainCatchOrder()
    {
        PrintHeading("2. Catch order — most specific first, most general last");

        SafeAccount account = new SafeAccount("Anita", 500m);
        decimal[] requests = { -5m, 900m, 100m };

        foreach (decimal request in requests)
        {
            try
            {
                account.Withdraw(request);
                Console.WriteLine($"Withdrew {request}; balance is now {account.Balance}");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine($"{request}: refused, that is not a positive amount");
            }
            catch (InsufficientFundsException ex)
            {
                Console.WriteLine($"{request}: refused, short by {ex.Shortfall}");
            }
            catch (Exception ex)
            {
                // The safety net for anything unexpected. It MUST come last: Exception matches
                // everything, so any clause placed after it could never be reached (the compiler rejects it, CS0160).
                Console.WriteLine($"{request}: unexpected problem — {ex.Message}");
            }
        }
    }

    // finally runs whether the try succeeded or threw — perfect for clean-up.
    public void ExplainFinally()
    {
        PrintHeading("3. finally — clean-up that always happens");

        SafeAccount account = new SafeAccount("Anita", 500m);
        decimal[] attempts = { 100m, 900m };
        int sessionsClosed = 0;

        foreach (decimal attempt in attempts)
        {
            try
            {
                account.Withdraw(attempt);
                Console.WriteLine($"Withdrew {attempt}");
            }
            catch (InsufficientFundsException ex)
            {
                Console.WriteLine($"Refused {attempt}: {ex.Message}");
            }
            finally
            {
                // Runs after the success AND after the failure — the "close the session" step.
                sessionsClosed++;
                Console.WriteLine($"Session {sessionsClosed} closed");
            }
        }
    }

    // `throw` raises an exception; a custom exception type can carry data to the catcher.
    public void ExplainCustomExceptions()
    {
        PrintHeading("4. Custom exceptions — throw your own, with data attached");

        SafeAccount account = new SafeAccount("Anita", 500m);

        try
        {
            // Inside Withdraw, `throw new InsufficientFundsException(...)` runs. Withdraw has no catch,
            // so the exception travels UP to this method, which does.
            account.Withdraw(900m);
        }
        catch (InsufficientFundsException ex)
        {
            decimal shortfall = ex.Shortfall; // the extra data our custom exception carries
            Console.WriteLine($"{account.Owner}'s withdrawal failed: short by {shortfall}");
            Console.WriteLine($"Balance is untouched: {account.Balance}");
        }
    }

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
    }
}
