using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.LinqFundamentals
{
    // Adapts LinqFundamentalsDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class LinqFundamentalsExample : ILearningTopic
    {
        public void Explain()
        {
            var linq = new LinqFundamentalsDemo();

            linq.ExplainWhereAndSelect();
            linq.ExplainOrderingAndAggregates();
            linq.ExplainAskingQuestions();
            linq.ExplainDeferredExecution();
        }
    }
}
