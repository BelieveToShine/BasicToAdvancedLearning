using BasicToAdvancedLearning.Interfaces;
using BasicToAdvancedLearning.MethodsAndParameters;

// Program.cs depends only on ILearningTopic, never on a concrete topic class. That
// keeps this file IDENTICAL no matter which topic is running — moving to the next
// lesson is a one-line change (swap the type on the right of "new"), and it doubles
// as a live example of polymorphism: the same topic.Explain() call below runs
// whatever concrete lesson is plugged in.
ILearningTopic topic = new MethodsAndParametersExample();
topic.Explain();
