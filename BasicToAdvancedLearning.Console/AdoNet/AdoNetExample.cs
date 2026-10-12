using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.AdoNet
{
    // Adapts AdoNetDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class AdoNetExample : ILearningTopic
    {
        public void Explain()
        {
            // Every method in this lesson talks to SQL Server, so stop early (with a friendly message)
            // when no connection string is configured or the server cannot be reached.
            if (!SqlLab.IsReady())
            {
                return;
            }

            var adoNet = new AdoNetDemo();

            adoNet.ExplainConnectionAndCommand();
            adoNet.ExplainParameters();
            adoNet.ExplainReadingRows();
            adoNet.ExplainTransactions();
        }
    }
}
