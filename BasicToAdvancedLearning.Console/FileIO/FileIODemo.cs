namespace BasicToAdvancedLearning.FileIO;

// Topic 8: File I/O basics — writing and reading text, streams with `using`, paths and existence
// checks, and what happens when a file is missing (Topic 7's exceptions at work). The running example
// is a tiny bank statement. Every method that creates a file uses ONE file in the operating system's temp
// folder and deletes it afterwards, so running the lesson never leaves anything behind (it would
// overwrite a file of the same name there, so the name is deliberately unusual). Each concept gets its own
// method so it can be explained and demoed one at a time from FileIOExample.
// See ArchitectureDiagrams/FileIO for the visual walkthrough.
public class FileIODemo
{
    // A name for the lesson's file, so the string is not repeated in every method.
    private const string StatementFileName = "bta-statement.txt";

    // The simplest way in: one call writes a whole string, one call reads it all back.
    public void ExplainWriteAndReadText()
    {
        PrintHeading("1. Write and read text — the whole file in one call");

        string path = GetStatementPath();
        string summary = "Anita: balance 500";

        try
        {
            // WriteAllText creates the file, or replaces it if it already exists.
            File.WriteAllText(path, summary);
            Console.WriteLine($"Wrote {summary.Length} characters to {Path.GetFileName(path)}");

            // ReadAllText opens the file, reads everything into a NEW string, and closes it again.
            string readBack = File.ReadAllText(path);
            Console.WriteLine($"Read back: {readBack}");
        }
        finally
        {
            File.Delete(path); // clean up, even if something above threw
        }
    }

    // A stream handles a file a bit at a time. `using` closes it for you, even after an error.
    public void ExplainLinesAndStreams()
    {
        PrintHeading("2. Streams — a file one line at a time, closed by using");

        string path = GetStatementPath();

        try
        {
            // A StreamWriter holds the file open while you write. The `using` block calls Dispose()
            // at its closing brace, which hands any buffered text to the operating system and releases the
            // file. Without `using`, the last buffered lines could be lost and (on Windows) the file stay locked.
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine("Deposit 200");
                writer.WriteLine("Withdraw 900 (refused)");
                writer.WriteLine("Withdraw 100");
            }

            int lineCount = 0;
            using (StreamReader reader = new StreamReader(path))
            {
                string? line;

                // ReadLine returns null once there is nothing left to read.
                while ((line = reader.ReadLine()) != null)
                {
                    lineCount++;
                    Console.WriteLine($"  line {lineCount}: {line}");
                }
            }

            Console.WriteLine($"Read {lineCount} lines");
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Paths are just strings, but Path.Combine and File.Exists make them safe to build and check.
    public void ExplainPathsAndExistence()
    {
        PrintHeading("3. Paths and existence — building an address and checking it");

        // These steps are kept as straight-line code so the three File.Exists answers are easy to compare
        // (methods 1 and 2 show the try/finally clean-up pattern).
        // Path.Combine joins the pieces with the right separator for this operating system.
        string folder = Path.GetTempPath();
        string path = Path.Combine(folder, StatementFileName);
        Console.WriteLine($"File name: {Path.GetFileName(path)}, extension: {Path.GetExtension(path)}");

        bool existedBefore = File.Exists(path);
        File.WriteAllText(path, "Anita: balance 500");
        bool existsNow = File.Exists(path);
        File.Delete(path);
        bool existsAfterDelete = File.Exists(path);

        Console.WriteLine($"Before writing: {existedBefore}, after writing: {existsNow}, after deleting: {existsAfterDelete}");
    }

    // Reading a missing file throws; catching it (Topic 7) keeps the program alive.
    public void ExplainFileErrors()
    {
        PrintHeading("4. File errors — a missing file is a normal surprise");

        string missingPath = Path.Combine(Path.GetTempPath(), "bta-no-such-file.txt");

        try
        {
            string text = File.ReadAllText(missingPath); // throws: there is no such file (a missing FOLDER would throw DirectoryNotFoundException instead)
            Console.WriteLine(text); // never runs
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Caught a {ex.GetType().Name}: that file does not exist");
        }

        // The other way: ask first. File.Exists never throws, but the answer can go stale before you act
        // on it (another program may delete the file), and it says false for a bad path too, so a catch can still be needed.
        bool canRead = File.Exists(missingPath);
        Console.WriteLine($"File.Exists says: {canRead} — checking first can avoid the exception");
    }

    // Builds the full path of the lesson's file inside the temp folder.
    private static string GetStatementPath()
    {
        return Path.Combine(Path.GetTempPath(), StatementFileName);
    }

    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', title.Length));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
    }
}
