using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.ApiTesting;

// Topic 19: Testing APIs with Swagger/OpenAPI and Postman. The running example is the in-memory bank API from
// BankApi.cs (accounts kept in a list, no database). Each method that needs the API starts its own fresh
// copy, so every method begins with the same two accounts (Asha Rao #1, Ben Carter #2).
//
// A real API is a separate program that keeps running; the lesson hosts it inside this console program only so
// the lesson can finish. Everything below talks to it with real HTTP requests, exactly like a browser, Swagger
// UI or Postman would. HttpClient calls are blocking here because async/await comes later.
// See ArchitectureDiagrams/ApiTesting for the visual walkthrough.
public class ApiTestingDemo
{
    private const string OpenApiPath = "/swagger/v1/swagger.json";
    private const string CollectionFileName = "bank-api.postman_collection.json";
    private const string HttpFileName = "bank-api.http";

    // OpenAPI is a description of the API written as data (JSON). It is the machine-readable part.
    public void ExplainWhatIsOpenApi()
    {
        PrintHeading("1. What is OpenAPI? — the API described as a JSON document");

        using RunningServer server = BankApi.Start();
        ApiReply reply = ApiClientHelpers.Send(server.Client, HttpMethod.Get, OpenApiPath);
        Console.WriteLine($"GET {OpenApiPath} -> {reply.Status} ({reply.ContentType})");

        // Read the document the way any tool would: as JSON. We only look; we do not assume more than we print.
        using JsonDocument document = JsonDocument.Parse(reply.Body);
        JsonElement root = document.RootElement;
        Console.WriteLine($"openapi: {root.GetProperty("openapi").GetString()}  (the OpenAPI format version; depends on the library)");
        Console.WriteLine($"info: title \"{root.GetProperty("info").GetProperty("title").GetString()}\", version \"{root.GetProperty("info").GetProperty("version").GetString()}\"");

        // "paths" holds one entry per URL; inside each, one entry per HTTP verb (an "operation").
        int operationCount = 0;
        foreach (JsonProperty path in root.GetProperty("paths").EnumerateObject())
        {
            Console.WriteLine($"  path {path.Name}");
            foreach (JsonProperty verb in path.Value.EnumerateObject())
            {
                operationCount++;
                string summary = verb.Value.TryGetProperty("summary", out JsonElement s) ? s.GetString()! : "(no summary)";
                Console.WriteLine($"    {verb.Name.ToUpperInvariant(),-6} {summary}");
            }
        }
        Console.WriteLine($"{operationCount} operations in total. The document describes the API; it does not run it.");
    }

    // Swagger UI is an ordinary web page. It is the human-friendly part that reads the OpenAPI document.
    public void ExplainSwaggerUi()
    {
        PrintHeading("2. Swagger UI — a web page built from the OpenAPI document");

        using RunningServer server = BankApi.Start();
        ApiReply page = ApiClientHelpers.Send(server.Client, HttpMethod.Get, "/swagger/index.html");
        Console.WriteLine($"GET /swagger/index.html -> {page.Status} ({page.ContentType})");

        // The HTML is only a frame: it lists no endpoints. It loads a script (index.js) whose settings name the
        // OpenAPI document, and the browser fetches that document and draws the endpoints.
        ApiReply script = ApiClientHelpers.Send(server.Client, HttpMethod.Get, "/swagger/index.js");
        Console.WriteLine($"  HTML mentions /api/accounts: {page.Body.Contains("/api/accounts")}  (as expected: the page is only a frame)");
        Console.WriteLine($"  GET /swagger/index.js -> {script.Status}; it names {OpenApiPath}: {script.Body.Contains(OpenApiPath)}");

        // /swagger on its own is only a redirect to the page above. A client that follows redirects
        // (HttpClient's default) never notices, so turn that off to see the redirect itself.
        using var noRedirect = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = server.Client.BaseAddress };
        ApiReply redirect = ApiClientHelpers.Send(noRedirect, HttpMethod.Get, "/swagger");
        Console.WriteLine($"GET /swagger -> {redirect.Status} (Location header: {redirect.Location})");

        // What a person does in the page (we looked at it in a real browser; this console cannot click):
        Console.WriteLine("In the browser: open an endpoint, click 'Try it out', fill in the parameters, click 'Execute'.");
        Console.WriteLine("The page then sends a real HTTP request and shows the curl command, URL, status and body.");
    }

    // The OpenAPI document is only as good as the descriptions the author adds to the code.
    public void ExplainDocumentingEndpoints()
    {
        PrintHeading("3. Documenting endpoints — attributes become response codes in the JSON");

        using RunningServer server = BankApi.Start();
        ApiReply reply = ApiClientHelpers.Send(server.Client, HttpMethod.Get, OpenApiPath);
        using JsonDocument document = JsonDocument.Parse(reply.Body);
        JsonElement paths = document.RootElement.GetProperty("paths");

        // Each response code listed under an operation came from a [ProducesResponseType] attribute,
        // except DELETE, which has none. Swashbuckle then lists only a default 200.
        foreach (string route in new[] { "/api/accounts", "/api/accounts/{id}", "/api/accounts/{id}/deposit" })
        {
            foreach (JsonProperty verb in paths.GetProperty(route).EnumerateObject())
            {
                string codes = string.Join(", ", verb.Value.GetProperty("responses").EnumerateObject().Select(r => r.Name));
                Console.WriteLine($"  {verb.Name.ToUpperInvariant(),-6} {route,-28} documents: {codes}");
            }
        }

        // The [EndpointDescription] text on GET /api/accounts/{id} also lands in the JSON.
        JsonElement getOne = paths.GetProperty("/api/accounts/{id}").GetProperty("get");
        Console.WriteLine($"  description of GET /api/accounts/{{id}}: {getOne.GetProperty("description").GetString()}");

        // The documentation is a description, not the behaviour: what the DELETE really returns is different.
        ApiReply deleted = ApiClientHelpers.Send(server.Client, HttpMethod.Delete, "/api/accounts/2");
        ApiReply deletedAgain = ApiClientHelpers.Send(server.Client, HttpMethod.Delete, "/api/accounts/2");
        Console.WriteLine($"  real DELETE /api/accounts/2 -> {deleted.Status}, and again -> {deletedAgain.Status} (the JSON says only 200)");

        // Validation attributes on the request class are documented too.
        JsonElement createSchema = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("CreateAccountRequest");
        Console.WriteLine($"  accountType pattern in the JSON: {createSchema.GetProperty("properties").GetProperty("accountType").GetProperty("pattern").GetString()}");
        ApiReply bad = ApiClientHelpers.Send(server.Client, HttpMethod.Post, "/api/accounts", "{\"owner\":\"Chen Li\",\"accountType\":\"Gold\"}");
        Console.WriteLine($"  real POST with accountType Gold -> {bad.Status}");
    }

    // A request is just data: method + URL + headers + body. Postman and .http files store that data.
    public void ExplainPostmanAndHttpFiles()
    {
        PrintHeading("4. Postman collection and .http file — saved requests, replayed");

        // Honest note: these two files are meant for an API you run as its own program. This lesson has no
        // such program, so it replays the same requests against the in-process copy of the API instead.
        Console.WriteLine("(The files are for an API run as its own program; this lesson has none, so it replays them against the in-process copy.)");

        string? collectionPath = ApiClientHelpers.FindShippedFile(CollectionFileName);
        string collectionText = collectionPath != null ? File.ReadAllText(collectionPath) : EmbeddedFiles.PostmanCollection;
        Console.WriteLine($"Loaded {CollectionFileName} from {(collectionPath != null ? "the file in the ApiTesting folder" : "the copy embedded in the program (file not found)")}");
        if (collectionPath != null) Console.WriteLine($"  embedded copy matches the file: {SameText(collectionText, EmbeddedFiles.PostmanCollection)}");

        using JsonDocument collection = JsonDocument.Parse(collectionText);
        JsonElement items = collection.RootElement.GetProperty("item");
        Console.WriteLine($"Collection \"{collection.RootElement.GetProperty("info").GetProperty("name").GetString()}\": {items.GetArrayLength()} requests");

        using RunningServer server = BankApi.Start();

        // Values a request hands to later requests (Postman: collection variables set by a test script).
        var captured = new Dictionary<string, string>();
        foreach (JsonElement item in items.EnumerateArray())
        {
            // {{baseUrl}} is a variable; the real address is only known now, so the replay fills it in.
            string rawUrl = item.GetProperty("request").GetProperty("url").GetProperty("raw").GetString()!;
            string path = ResolveVariables(rawUrl, collection.RootElement, server.BaseAddress, captured);
            int expected = ExpectedStatus(item);
            ApiReply actual = SendCollectionRequest(server.Client, item.GetProperty("request"), path);
            CaptureVariables(item, actual, captured);
            string verdict = actual.Status == expected ? "match" : "MISMATCH";
            Console.WriteLine($"  {item.GetProperty("request").GetProperty("method").GetString(),-6} {PathOnly(path),-28} expected {expected}, actual {actual.Status}  {verdict}");
        }

        PrintHttpFileComparison(items);
    }

    // A check is "send a request, compare the answer with what we expect". Doing it by hand is manual
    // testing; writing the comparison as code that prints PASS or FAIL is automated testing.
    public void ExplainManualVsAutomatedTests()
    {
        PrintHeading("5. Manual vs automated — the same check, written as code");

        using RunningServer server = BankApi.Start();
        HttpClient client = server.Client;
        int passed = 0;
        int failed = 0;

        ApiReply list = ApiClientHelpers.Send(client, HttpMethod.Get, "/api/accounts");
        Report("GET /api/accounts returns 200", list.Status == 200, $"status {list.Status}", ref passed, ref failed);
        using (JsonDocument listJson = JsonDocument.Parse(list.Body))
            Report("  ...and lists 2 accounts", listJson.RootElement.GetArrayLength() == 2, $"{listJson.RootElement.GetArrayLength()} accounts", ref passed, ref failed);

        ApiReply created = ApiClientHelpers.Send(client, HttpMethod.Post, "/api/accounts", "{\"owner\":\"Chen Li\",\"accountType\":\"Savings\"}");
        Report("POST /api/accounts returns 201", created.Status == 201, $"status {created.Status}", ref passed, ref failed);
        Report("  ...with a Location header /api/accounts/3", created.Location == "/api/accounts/3", $"Location {created.Location}", ref passed, ref failed);

        ApiReply deposit = ApiClientHelpers.Send(client, HttpMethod.Post, "/api/accounts/1/deposit", "{\"amount\":50.25}");
        using (JsonDocument depositJson = JsonDocument.Parse(deposit.Body))
        {
            decimal balance = depositJson.RootElement.GetProperty("balance").GetDecimal();
            Report("Deposit 50.25 into account 1 gives balance 550.25", balance == 550.25m, $"balance {balance}", ref passed, ref failed);
        }

        // On purpose wrong: account 999 does not exist, so the API answers 404, not 200.
        ApiReply missing = ApiClientHelpers.Send(client, HttpMethod.Get, "/api/accounts/999");
        Report("(wrong on purpose) GET /api/accounts/999 returns 200", missing.Status == 200, $"status {missing.Status}", ref passed, ref failed);

        Console.WriteLine($"{passed} passed, {failed} failed (the one failure is the deliberately wrong check)");
        Console.WriteLine("Real test projects usually use xUnit with WebApplicationFactory, which runs the API in memory");
        Console.WriteLine("inside the test and needs no port (not used in this lesson, so not run here).");
    }

    // ---- small helpers (no ExplainX here) ------------------------------------------------------

    private static void PrintHeading(string text)
    {
        Console.WriteLine();
        Console.WriteLine(text);
    }

    // Prints one PASS/FAIL line and counts it.
    private static void Report(string name, bool ok, string detail, ref int passed, ref int failed)
    {
        if (ok) passed++; else failed++;
        Console.WriteLine($"  {(ok ? "PASS" : "FAIL")}  {name}  [{detail}]");
    }

    // Replaces {{name}} with the collection's variable of that name; {{baseUrl}} gets the live address.
    private static string ResolveVariables(string text, JsonElement collectionRoot, string baseAddress, Dictionary<string, string> captured)
    {
        return Regex.Replace(text, @"\{\{(\w+)\}\}", match =>
        {
            string name = match.Groups[1].Value;
            if (name == "baseUrl") return baseAddress.TrimEnd('/');
            if (captured.TryGetValue(name, out string? value)) return value;
            foreach (JsonElement variable in collectionRoot.GetProperty("variable").EnumerateArray())
                if (variable.GetProperty("key").GetString() == name) return variable.GetProperty("value").GetString()!;
            return match.Value;
        });
    }

    // Postman keeps its checks as JavaScript in the item's "test" event, e.g. pm.response.to.have.status(201).
    // The lesson does not run JavaScript; it only reads the number out of that line.
    private static int ExpectedStatus(JsonElement item)
    {
        Match m = Regex.Match(ScriptOf(item), @"have\.status\((\d+)\)");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    // Recognises one script line shape: pm.collectionVariables.set("name", pm.response.json().property);
    // and stores that property of the reply under that name, so the next requests can use {{name}}.
    private static void CaptureVariables(JsonElement item, ApiReply reply, Dictionary<string, string> captured)
    {
        Match m = Regex.Match(ScriptOf(item), @"collectionVariables\.set\(""(\w+)"",\s*pm\.response\.json\(\)\.(\w+)\)");
        if (!m.Success) return;
        using JsonDocument body = JsonDocument.Parse(reply.Body);
        captured[m.Groups[1].Value] = body.RootElement.GetProperty(m.Groups[2].Value).ToString();
    }

    // All the script lines of the item's test event, joined into one text.
    private static string ScriptOf(JsonElement item)
    {
        var lines = new List<string>();
        foreach (JsonElement evt in item.GetProperty("event").EnumerateArray())
            lines.AddRange(evt.GetProperty("script").GetProperty("exec").EnumerateArray().Select(l => l.GetString()!));
        return string.Join("\n", lines);
    }

    // Rebuilds the request from the collection's data: method, URL, headers, body.
    private static ApiReply SendCollectionRequest(HttpClient client, JsonElement request, string url)
    {
        using var message = new HttpRequestMessage(new HttpMethod(request.GetProperty("method").GetString()!), url);
        string mediaType = "application/json";
        foreach (JsonElement header in request.GetProperty("header").EnumerateArray())
        {
            if (header.GetProperty("key").GetString() == "Content-Type") mediaType = header.GetProperty("value").GetString()!;
            else message.Headers.TryAddWithoutValidation(header.GetProperty("key").GetString()!, header.GetProperty("value").GetString());
        }
        if (request.TryGetProperty("body", out JsonElement body))
            message.Content = new StringContent(body.GetProperty("raw").GetString()!, Encoding.UTF8, mediaType);
        return ApiClientHelpers.Read(client.Send(message));
    }

    // Compares two texts ignoring line-ending style and trailing blank lines.
    private static bool SameText(string a, string b) => a.Replace("\r\n", "\n").Trim() == b.Replace("\r\n", "\n").Trim();

    private static string PathOnly(string url) => new Uri(url).AbsolutePath;

    // The .http file holds the same requests in a plainer text form. Read it and check it lists the same
    // method + URL lines as the collection (this checks the two files agree; it does not send them).
    private static void PrintHttpFileComparison(JsonElement collectionItems)
    {
        string? httpPath = ApiClientHelpers.FindShippedFile(HttpFileName);
        string httpText = httpPath != null ? File.ReadAllText(httpPath) : EmbeddedFiles.HttpFile;
        Console.WriteLine($"Loaded {HttpFileName} from {(httpPath != null ? "the file in the ApiTesting folder" : "the copy embedded in the program (file not found)")}");

        if (httpPath != null) Console.WriteLine($"  embedded copy matches the file: {SameText(httpText, EmbeddedFiles.HttpFile)}");

        // The .http file takes the new account's id from the createAccount reply; the collection calls it newAccountId.
        httpText = httpText.Replace("{{createAccount.response.body.$.id}}", "{{newAccountId}}");
        List<string> fromHttp = Regex.Matches(httpText, @"(?m)^(GET|POST|PUT|DELETE|PATCH) (\S+)\s*$").Select(m => $"{m.Groups[1].Value} {m.Groups[2].Value}").ToList();
        List<string> fromCollection = collectionItems.EnumerateArray()
            .Select(i => $"{i.GetProperty("request").GetProperty("method").GetString()} {i.GetProperty("request").GetProperty("url").GetProperty("raw").GetString()}").ToList();
        Console.WriteLine($".http file: {fromHttp.Count} requests; same method + URL list as the collection: {fromHttp.SequenceEqual(fromCollection)}");
    }
}
