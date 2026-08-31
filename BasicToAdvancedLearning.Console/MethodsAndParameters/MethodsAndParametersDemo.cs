namespace BasicToAdvancedLearning.MethodsAndParameters;

// Topic 3: Methods & Parameters — declaring/calling methods, value vs ref vs out
// parameters, optional/named parameters, params, and overloading. Each concept
// gets its own method so it can be explained and demoed one at a time from
// MethodsAndParametersExample. See ArchitectureDiagrams/MethodsAndParameters for
// the visual walkthrough.
public class MethodsAndParametersDemo
{
    public void ExplainMethodBasics()
    {
        PrintHeading("1. Method Basics");

        // A method groups reusable code under a name. Parameters go in; a return
        // value (if any) comes back out. void means "no return value."
        int total = AddNumbers(5, 3);
        Console.WriteLine($"AddNumbers(5, 3) returned {total}");

        Greet("Vivek");
    }

    public void ExplainParameters()
    {
        PrintHeading("2. Parameters — Value vs ref vs out");

        // Passed BY VALUE (the default): the method gets its own COPY. Changes
        // inside the method never affect the caller's variable.
        int number = 10;
        TryDoubleByValue(number);
        Console.WriteLine($"After TryDoubleByValue: number = {number}  (unchanged)");

        // Passed by REF: the method receives the caller's actual variable, not a
        // copy — changes inside the method DO affect the caller's variable. The
        // caller must initialize the variable before passing it by ref.
        DoubleByRef(ref number);
        Console.WriteLine($"After DoubleByRef: number = {number}  (changed!)");

        // Passed by OUT: like ref, but the method is REQUIRED to assign it before
        // returning — used for "extra return values" (this is exactly how
        // int.TryParse hands back its parsed number back in Programming Basics).
        ParseInput("42", out int parsed);
        Console.WriteLine($"ParseInput out result: parsed = {parsed}");
    }

    public void ExplainOptionalAndNamedParameters()
    {
        PrintHeading("3. Optional & Named Parameters");

        // A parameter with a default value becomes optional — callers can omit it.
        Console.WriteLine(BuildGreeting("Vivek"));
        Console.WriteLine(BuildGreeting("Vivek", "Good morning"));

        // Named arguments let a caller specify parameters by name, in any order —
        // handy once a method has several optional parameters.
        Console.WriteLine(BuildGreeting(greeting: "Welcome", name: "Vivek"));
    }

    public void ExplainParamsKeyword()
    {
        PrintHeading("4. The params Keyword");

        // params lets a caller pass any number of arguments (including zero)
        // without building an array themselves — C# packs them into one for you.
        Console.WriteLine($"Sum() = {Sum()}");
        Console.WriteLine($"Sum(1, 2, 3) = {Sum(1, 2, 3)}");
        Console.WriteLine($"Sum(10, 20, 30, 40) = {Sum(10, 20, 30, 40)}");
    }

    public void ExplainMethodOverloading()
    {
        PrintHeading("5. Method Overloading");

        // Overloading = same method name, different parameter list. The compiler
        // picks the matching overload from the arguments you pass — never from
        // what you do with the return value.
        Console.WriteLine($"Describe(5) -> {Describe(5)}");
        Console.WriteLine($"Describe(5, 10) -> {Describe(5, 10)}");
        Console.WriteLine($"Describe(\"Vivek\") -> {Describe("Vivek")}");
    }

    private static int AddNumbers(int a, int b)
    {
        return a + b;
    }

    private static void Greet(string name)
    {
        Console.WriteLine($"Hello, {name}!");
    }

    private static void TryDoubleByValue(int value)
    {
        value *= 2; // only changes this method's own copy
    }

    private static void DoubleByRef(ref int value)
    {
        value *= 2; // changes the caller's actual variable
    }

    private static void ParseInput(string text, out int result)
    {
        result = int.Parse(text); // must be assigned before this method returns
    }

    private static string BuildGreeting(string name, string greeting = "Hello")
    {
        return $"{greeting}, {name}!";
    }

    private static int Sum(params int[] numbers)
    {
        int total = 0;
        foreach (int n in numbers)
        {
            total += n;
        }
        return total;
    }

    private static string Describe(int number) => $"a single number: {number}";
    private static string Describe(int a, int b) => $"a range: {a} to {b}";
    private static string Describe(string text) => $"a piece of text: \"{text}\"";

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
    }
}
