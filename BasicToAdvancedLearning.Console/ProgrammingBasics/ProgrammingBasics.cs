namespace BasicToAdvancedLearning.ProgrammingBasics;

// Topic 1: Programming Basics — variables, data types, operators, input/output.
// Each concept gets its own method so it can be explained and demoed one at a time
// from Program.cs. See ArchitectureDiagrams/ProgrammingBasics for the visual walkthrough.
public class ProgrammingBasicsDemo
{
    public void ExplainVariablesAndDataTypes()
    {
        PrintHeading("1. Variables & Data Types");

        // A variable is a named slot in memory. Its data type decides what
        // kind of value it can hold, how much memory it uses, and what
        // operations are valid on it.
        int age = 28;                  // whole numbers
        double price = 499.99;         // fractional numbers (approximate, fast)
        decimal salary = 55000.50m;    // fractional numbers needing exact precision — use for money; note the 'm' suffix
        bool isActive = true;          // true / false only
        char grade = 'A';              // exactly one character — single quotes
        string name = "Vivek";         // text — double quotes

        // 'var' asks the compiler to infer the type from the value on the right.
        // The variable is still strongly typed after this — it's just less typing.
        var country = "India";

        Console.WriteLine($"age (int)        = {age}");
        Console.WriteLine($"price (double)   = {price}");
        Console.WriteLine($"salary (decimal) = {salary}");
        Console.WriteLine($"isActive (bool)  = {isActive}");
        Console.WriteLine($"grade (char)     = {grade}");
        Console.WriteLine($"name (string)    = {name}");
        Console.WriteLine($"country (var, inferred as string) = {country}");

        Console.WriteLine();
        Console.WriteLine("Rule of thumb: int for counting, decimal for money, double for scientific/graphics math.");
    }

    public void ExplainOperators()
    {
        PrintHeading("2. Operators");

        int a = 10;
        int b = 3;

        Console.WriteLine("-- Arithmetic: perform a calculation --");
        Console.WriteLine($"{a} + {b} = {a + b}");
        Console.WriteLine($"{a} - {b} = {a - b}");
        Console.WriteLine($"{a} * {b} = {a * b}");
        Console.WriteLine($"{a} / {b} = {a / b}   <- both operands are int, so the result truncates (no decimals)");
        Console.WriteLine($"{a} % {b} = {a % b}   <- remainder left over after division");

        Console.WriteLine();
        Console.WriteLine("-- Comparison: always produce true/false --");
        Console.WriteLine($"{a} > {b}  = {a > b}");
        Console.WriteLine($"{a} == {b} = {a == b}");
        Console.WriteLine($"{a} != {b} = {a != b}");

        bool hasTicket = true;
        bool hasId = false;

        Console.WriteLine();
        Console.WriteLine("-- Logical: combine true/false values --");
        Console.WriteLine($"hasTicket && hasId = {hasTicket && hasId}   <- AND: both sides must be true");
        Console.WriteLine($"hasTicket || hasId = {hasTicket || hasId}   <- OR: at least one side must be true");
        Console.WriteLine($"!hasId             = {!hasId}   <- NOT: flips the value");

        Console.WriteLine();
        Console.WriteLine("-- Assignment: store or update a value --");
        int score = 5;
        Console.WriteLine($"score        = {score}");
        score += 3; // shorthand for: score = score + 3
        Console.WriteLine($"score += 3  -> {score}");
    }

    public void ExplainInputOutput()
    {
        PrintHeading("3. Input & Output");

        // Console.Write / Console.WriteLine send text OUT to the screen.
        Console.Write("Enter your name: ");

        // Console.ReadLine() reads text IN from the user — it ALWAYS returns a string,
        // even if the user types a number.
        string? enteredName = Console.ReadLine();

        Console.Write("Enter your age: ");
        string? enteredAgeText = Console.ReadLine();

        // To do math with the typed-in age we must convert the string to a number first.
        // int.TryParse attempts the conversion and returns true/false instead of throwing,
        // which is the safe way to handle input that might not be a valid number.
        if (int.TryParse(enteredAgeText, out int enteredAge))
        {
            Console.WriteLine();
            Console.WriteLine($"Hello {enteredName}, next year you will be {enteredAge + 1}.");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine($"Hello {enteredName}, \"{enteredAgeText}\" isn't a valid number, so we can't calculate your next age.");
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
