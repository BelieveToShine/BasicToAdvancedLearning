using BasicToAdvancedLearning.Interfaces;
using BasicToAdvancedLearning.LinqFundamentals;

// Program.cs depends only on ILearningTopic, never on a concrete topic class. That
// keeps this file nearly identical no matter which topic is running — to run a different
// lesson you change exactly TWO lines: the `using` line (the topic's namespace) and the
// type on the right of "new" — and it doubles
// as a live example of polymorphism: the same topic.Explain() call below runs
// whatever concrete lesson is plugged in.
ILearningTopic topic = new LinqFundamentalsExample();
topic.Explain();
