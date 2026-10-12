using System.ComponentModel.DataAnnotations;

namespace BasicToAdvancedLearning.WebApiCrud;

// DTO = Data Transfer Object: a class shaped for the HTTP conversation, not for the database.

// What the API SENDS back. Built from the Accounts table joined to the owner's name.
public class AccountDto
{
    public int AccountId { get; set; }
    public string CustomerName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountType { get; set; } = "";
    public decimal Balance { get; set; }
}

// What the client SENDS to open an account (POST). The attributes are the validation rules: with
// [ApiController] on the controller, a request that breaks one never reaches the action method.
// Each rule carries its own ErrorMessage so the text the client sees is ours, not a default.
public class CreateAccountRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "CustomerId must be 1 or more.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "AccountNumber is required.")]
    [StringLength(20, MinimumLength = 6, ErrorMessage = "AccountNumber must be 6 to 20 characters.")]
    public string AccountNumber { get; set; } = "";

    [Required(ErrorMessage = "AccountType is required.")]
    [AllowedValues("Savings", "Current", ErrorMessage = "AccountType must be Savings or Current.")]
    public string AccountType { get; set; } = "";

    [Range(0, 1000000, ErrorMessage = "Balance must be between 0 and 1000000.")]
    public decimal Balance { get; set; }
}

// What the client SENDS to replace an account (PUT). It has no CustomerId: an account does not move
// to another customer, and leaving the field out means the client cannot even try.
public class ReplaceAccountRequest
{
    [Required(ErrorMessage = "AccountNumber is required.")]
    [StringLength(20, MinimumLength = 6, ErrorMessage = "AccountNumber must be 6 to 20 characters.")]
    public string AccountNumber { get; set; } = "";

    [Required(ErrorMessage = "AccountType is required.")]
    [AllowedValues("Savings", "Current", ErrorMessage = "AccountType must be Savings or Current.")]
    public string AccountType { get; set; } = "";

    [Range(0, 1000000, ErrorMessage = "Balance must be between 0 and 1000000.")]
    public decimal Balance { get; set; }
}
