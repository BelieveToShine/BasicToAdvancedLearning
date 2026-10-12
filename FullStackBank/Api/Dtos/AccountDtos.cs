using System.ComponentModel.DataAnnotations;

namespace FullStackBank.Api.Dtos;

// DTO = Data Transfer Object: the exact shape of the JSON that crosses the wire. Separate from the entities
// so the API contract (what clients depend on) can stay stable while the database changes.
//
// Validation attributes ([Required], [Range], [RegularExpression]...) are checked by ASP.NET Core BEFORE the action runs,
// because the controller is marked [ApiController]: if anything is invalid, the framework itself answers
// 400 with a ProblemDetails body and the action never executes.

// What the API sends back for an account.
// Field names are a CONTRACT with the React lessons (same shape as the WebApiCrud topic's AccountDto): do not rename.
public record AccountDto(
    int AccountId,
    string CustomerName,
    string AccountNumber,
    string AccountType,
    decimal Balance);

// What a client sends to open an account. The balance is NOT here: a new account always starts at 0 and
// money arrives only through transactions, so the balance can never be set to an arbitrary number.
public class CreateAccountRequest
{
    // [Range] on an int: a missing JSON property would otherwise become 0 silently, so 1..max rejects it.
    [Range(1, int.MaxValue, ErrorMessage = "customerId must be a valid customer id (1 or more).")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "accountNumber is required.")]
    [RegularExpression("^[A-Za-z0-9-]{1,20}$", ErrorMessage = "accountNumber must be 1-20 characters: letters, digits or '-'.")]
    public string? AccountNumber { get; set; }

    // [AllowedValues] also rejects null, so a missing accountType gets the same clear message.
    [AllowedValues("Savings", "Current", ErrorMessage = "accountType must be 'Savings' or 'Current'.")]
    public string? AccountType { get; set; }
}
