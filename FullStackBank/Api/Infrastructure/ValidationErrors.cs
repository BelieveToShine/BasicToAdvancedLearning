using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FullStackBank.Api.Infrastructure;

// By default [ApiController] answers an invalid request with 400 + ProblemDetails, but its "errors" keys are the
// C# property names ("AccountNumber"). The JSON the client SENT uses camelCase ("accountNumber"), so a front end
// would have to translate. This factory keeps the framework's automatic 400 (same status, same body shape) and only
// renames the keys to camelCase, so errors.accountNumber matches the field the user typed into.
public static class ValidationErrors
{
    public static IActionResult CamelCaseResponse(ActionContext context)
    {
        var errors = new Dictionary<string, string[]>();
        foreach ((string key, ModelStateEntry entry) in context.ModelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            // A bad or missing body also produces a vague "The request field is required." entry next to the
            // real JSON error ("$" or "$.something"); the vague one only adds noise.
            if (key == "request" && context.ModelState.Keys.Any(k => k.StartsWith('$') || k.Length == 0))
            {
                continue;
            }

            string[] messages = key.StartsWith('$')
                // The framework's own text names .NET types and byte positions ("could not be converted to System.Decimal. Path: ...").
                // That is noise for a client and leaks implementation detail, so say it plainly instead.
                ? new[] { WrongFormatMessage(key) }
                : entry.Errors.Select(e => e.ErrorMessage).ToArray();
            // An empty or missing body is reported under the empty key; give it a readable name.
            errors[key.Length == 0 ? "body" : CamelCase(key)] = messages;
        }

        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        ValidationProblemDetails problem = factory.CreateValidationProblemDetails(
            context.HttpContext, new ModelStateDictionary(), StatusCodes.Status400BadRequest);
        foreach (var (key, messages) in errors)
        {
            problem.Errors[key] = messages;
        }

        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }

    private static string WrongFormatMessage(string path) =>
        path == "$"
            ? "The request body is not valid JSON."
            : $"The value for '{path[(path.LastIndexOf('.') + 1)..]}' has the wrong type or format.";

    // "AccountNumber" -> "accountNumber". Keys that are JSON paths ("$.accountType") are already in the client's spelling.
    private static string CamelCase(string key) =>
        key.Length == 0 || key[0] == '$' || char.IsLower(key[0]) ? key : char.ToLowerInvariant(key[0]) + key[1..];
}
