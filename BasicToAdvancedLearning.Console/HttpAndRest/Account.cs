namespace BasicToAdvancedLearning.HttpAndRest;

// One bank account, the "thing" the lesson's server keeps and sends. A record is a short way to write a
// class that is mostly data: three properties that are set once when the object is created.
public record Account(int Id, string Owner, decimal Balance);

// What a client sends to create or replace an account. It has no Id: the SERVER picks the id.
public record AccountInput(string? Owner, decimal Balance);
