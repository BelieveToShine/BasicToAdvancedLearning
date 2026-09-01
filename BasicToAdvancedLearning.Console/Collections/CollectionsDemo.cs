namespace BasicToAdvancedLearning.Collections;

// Topic 4: Collections — arrays, List<T>, and Dictionary<TKey, TValue>. Same
// three examples run through every method so the ideas connect: exam scores
// (Array), a to-do list (List<T>), a phone book (Dictionary). Each concept gets
// its own method so it can be explained and demoed one at a time from
// CollectionsExample. See ArchitectureDiagrams/Collections for the visual walkthrough.
public class CollectionsDemo
{
    public void ExplainArrays()
    {
        PrintHeading("1. Arrays — a fixed-size row of boxes");

        // An array's SIZE is locked in the moment it's created. Great when you
        // know exactly how many items there'll be — like 5 students' exam scores.
        int[] examScores = { 85, 92, 78, 60, 95 };

        Console.WriteLine($"Number of scores (Length): {examScores.Length}");
        Console.WriteLine($"First score (index 0): {examScores[0]}");
        Console.WriteLine($"Last score (index {examScores.Length - 1}): {examScores[examScores.Length - 1]}");

        // You CAN overwrite an existing slot...
        examScores[2] = 80; // student 3's paper got re-checked
        Console.WriteLine($"Score at index 2, after a re-check: {examScores[2]}");

        // ...but you can never add a 6th score or remove one — 5 slots, forever.
        Console.WriteLine("All scores:");
        foreach (int score in examScores)
        {
            Console.WriteLine($"  {score}");
        }
    }

    public void ExplainLists()
    {
        PrintHeading("2. List<T> — a row of boxes that can grow and shrink");

        // Unlike an array, a List<T> resizes itself as you add or remove items —
        // exactly what a real to-do list needs, since you don't know the count upfront.
        List<string> todoList = new List<string>();

        todoList.Add("Buy groceries");
        todoList.Add("Finish homework");
        todoList.Add("Call mom");
        Console.WriteLine($"Tasks after adding 3: {todoList.Count}");

        todoList.Insert(1, "Walk the dog"); // slot a task into a specific position
        Console.WriteLine($"Tasks after inserting one: {todoList.Count}");

        todoList.Remove("Finish homework"); // done! remove by value, not position
        Console.WriteLine($"Tasks after removing one: {todoList.Count}");

        Console.WriteLine($"Contains \"Call mom\"? {todoList.Contains("Call mom")}");
        Console.WriteLine($"Task at index 0: {todoList[0]}");

        Console.WriteLine("Remaining tasks:");
        foreach (string task in todoList)
        {
            Console.WriteLine($"  - {task}");
        }
    }

    public void ExplainDictionaries()
    {
        PrintHeading("3. Dictionary<TKey, TValue> — look things up by name, not position");

        // A Dictionary maps a unique KEY to a VALUE — like a phone book: you look
        // someone up by name, never by "the 3rd entry."
        Dictionary<string, string> phoneBook = new Dictionary<string, string>();

        phoneBook.Add("Vivek", "9876543210");
        phoneBook["Anita"] = "9123456789"; // the indexer can add too, not just read

        Console.WriteLine($"Contacts saved: {phoneBook.Count}");
        Console.WriteLine($"Vivek's number: {phoneBook["Vivek"]}");
        Console.WriteLine($"Do we have Rahul's number? {phoneBook.ContainsKey("Rahul")}");

        Console.WriteLine("Everyone in the phone book:");
        foreach (KeyValuePair<string, string> contact in phoneBook)
        {
            Console.WriteLine($"  {contact.Key} -> {contact.Value}");
        }
    }

    public void ExplainCollectionSafety()
    {
        PrintHeading("4. Collection Safety — check before you leap");

        // Indexing past the end of an array (examScores[10] here) throws
        // IndexOutOfRangeException. Checking against Length first avoids it entirely.
        int[] examScores = { 85, 92, 78, 60, 95 };
        int indexToCheck = 10;

        if (indexToCheck >= 0 && indexToCheck < examScores.Length)
        {
            Console.WriteLine($"Score at index {indexToCheck}: {examScores[indexToCheck]}");
        }
        else
        {
            Console.WriteLine($"Index {indexToCheck} is out of range (valid: 0 to {examScores.Length - 1}).");
        }

        Dictionary<string, string> phoneBook = new Dictionary<string, string>
        {
            { "Vivek", "9876543210" },
            { "Anita", "9123456789" }
        };

        // phoneBook["Rahul"] would throw KeyNotFoundException, because "Rahul"
        // was never added. TryGetValue is the Dictionary's version of int.TryParse
        // from Programming Basics — it returns true/false instead of throwing.
        if (phoneBook.TryGetValue("Rahul", out string? rahulNumber))
        {
            Console.WriteLine($"Rahul's number: {rahulNumber}");
        }
        else
        {
            Console.WriteLine("Rahul isn't in the phone book.");
        }
    }

    public void ExplainChoosingACollection()
    {
        PrintHeading("5. Choosing the Right Collection");

        Console.WriteLine("Know the exact count, and it won't change?      -> Array        (examScores)");
        Console.WriteLine("Items get added/removed over time?              -> List<T>      (todoList)");
        Console.WriteLine("Need to look things up by a unique name/id?     -> Dictionary<K,V> (phoneBook)");
    }

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
    }
}
