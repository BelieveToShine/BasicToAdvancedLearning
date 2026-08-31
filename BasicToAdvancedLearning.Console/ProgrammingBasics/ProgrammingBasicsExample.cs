using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.ProgrammingBasics
{
    // Adapts ProgrammingBasicsDemo (the actual lesson code) to the ILearningTopic
    // contract Program.cs depends on. The demo class itself has no knowledge of the
    // interface — this wrapper is the only thing that needs to change if the demo's
    // shape ever changes, so the lesson code stays free of "runner" concerns.
    internal class ProgrammingBasicsExample : ILearningTopic
    {
        public void Explain()
        {
            var programmingBasics = new ProgrammingBasicsDemo();

            programmingBasics.ExplainVariablesAndDataTypes();
            programmingBasics.ExplainOperators();
            programmingBasics.ExplainInputOutput();
        }
    }
}
