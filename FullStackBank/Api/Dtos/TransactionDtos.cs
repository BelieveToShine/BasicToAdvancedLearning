using System.ComponentModel.DataAnnotations;

namespace FullStackBank.Api.Dtos;

// One row of an account's history. Also the body of POST .../transactions (a contract with the React lessons: do not rename).
// The new balance is not repeated here; read the account (GET /api/accounts/{id}) to see it.
public record TransactionDto(
    long TransactionId,
    string Kind,
    decimal Amount,
    DateTime CreatedAt);

public class CreateTransactionRequest : IValidatableObject
{
    [AllowedValues("Deposit", "Withdrawal", ErrorMessage = "kind must be 'Deposit' or 'Withdrawal'.")]
    public string? Kind { get; set; }

    // Amount is always positive; the KIND says which way the money moves. "-50 deposit" is therefore invalid
    // rather than quietly meaning a withdrawal. The upper limit keeps the value inside DECIMAL(18,2).
    [Range(0.01, 1_000_000_000_000, ErrorMessage = "amount must be between 0.01 and 1,000,000,000,000.00.")]
    public decimal Amount { get; set; }

    // Runs only after the attribute checks above all pass.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Money has 2 decimal places in the database; refuse 10.005 instead of silently rounding it.
        if (decimal.Round(Amount, 2) != Amount)
        {
            yield return new ValidationResult("amount can have at most 2 decimal places.", new[] { nameof(Amount) });
        }
    }
}
