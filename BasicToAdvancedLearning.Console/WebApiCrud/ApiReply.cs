namespace BasicToAdvancedLearning.WebApiCrud;

// What the lesson keeps from one HTTP response: the number, the body text, and the Location header.
public record ApiReply(int StatusCode, string Body, string? LocationPath);
