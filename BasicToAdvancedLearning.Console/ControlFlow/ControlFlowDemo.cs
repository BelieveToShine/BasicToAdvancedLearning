namespace BasicToAdvancedLearning.ControlFlow;

// Topic 2: Control Flow — if/else, switch, and the four loop shapes (for, while,
// do-while, foreach). Each concept gets its own method so it can be explained and
// demoed one at a time from ControlFlowExample. See ArchitectureDiagrams/ControlFlow
// for the visual walkthrough — loops each get their own Flow + Memory diagram there.
public class ControlFlowDemo
{
    public void ExplainIfElse()
    {
        PrintHeading("1. If / Else If / Else");

        // if checks conditions top-to-bottom; the FIRST one that's true runs and the
        // rest are skipped. else only runs when none of the conditions above matched.
        int temperature = 42;

        if (temperature > 35)
        {
            Console.WriteLine($"{temperature}°C — it's hot outside.");
        }
        else if (temperature > 20)
        {
            Console.WriteLine($"{temperature}°C — it's a pleasant day.");
        }
        else
        {
            Console.WriteLine($"{temperature}°C — it's cold outside.");
        }
    }

    public void ExplainSwitch()
    {
        PrintHeading("2. Switch");

        // switch jumps straight to the matching case instead of checking conditions
        // one by one. Unlike C/Java, C# does NOT fall through to the next case by
        // default — every case must break/return/throw (or you can list several
        // case labels together, like 6/7 below, to share one block).
        int dayNumber = 3;

        switch (dayNumber)
        {
            case 1:
                Console.WriteLine("Monday");
                break;
            case 2:
                Console.WriteLine("Tuesday");
                break;
            case 3:
                Console.WriteLine("Wednesday");
                break;
            case 6:
            case 7:
                Console.WriteLine("Weekend");
                break;
            default:
                Console.WriteLine("Not a valid day number");
                break;
        }
    }

    public void ExplainForLoop()
    {
        PrintHeading("3. For Loop");

        // for has three parts: initializer (runs once, before anything else),
        // condition (checked BEFORE every iteration, including the first — false
        // from the start means the body never runs), and increment (runs AFTER the
        // body, right before the next condition check).
        Console.WriteLine("Counting 1 to 5:");
        for (int i = 1; i <= 5; i++)
        {
            Console.WriteLine($"  i = {i}");
        }
    }

    public void ExplainWhileLoop()
    {
        PrintHeading("4. While Loop");

        // while checks its condition BEFORE each iteration, same as for's condition —
        // so it can run zero times if the condition starts out false.
        Console.WriteLine("Counting down:");
        int countdown = 3;
        while (countdown > 0)
        {
            Console.WriteLine($"  {countdown}...");
            countdown--;
        }
        Console.WriteLine("  Liftoff!");
    }

    public void ExplainDoWhileLoop()
    {
        PrintHeading("5. Do-While Loop");

        // do-while checks its condition AFTER the body — the body always runs at
        // least once, even if the condition is false from the start. That's the one
        // real difference from while.
        Console.WriteLine("do-while always runs at least once:");
        int attempts = 0;
        do
        {
            attempts++;
            Console.WriteLine($"  Attempt #{attempts}");
        } while (attempts < 3);
    }

    public void ExplainForeach()
    {
        PrintHeading("6. Foreach Loop");

        // foreach walks a collection element by element via its enumerator — no
        // manual index, and you can't modify the collection while iterating it.
        string[] fruits = { "Apple", "Banana", "Cherry" };

        Console.WriteLine("Walking through an array:");
        foreach (string fruit in fruits)
        {
            Console.WriteLine($"  {fruit}");
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
