namespace BasicToAdvancedLearning.Interfaces;

// Every topic (ProgrammingBasics, and whatever comes after it) implements this so
// Program.cs never needs to change shape when a new topic is added — see Program.cs
// for why that matters.
internal interface ILearningTopic
{
    void Explain();
}
