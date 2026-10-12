using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.SqlBasics
{
    // Adapts SqlBasicsDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class SqlBasicsExample : ILearningTopic
    {
        public void Explain()
        {
            // This lesson needs a SQL Server. If none is configured, IsReady() prints why and we stop.
            if (!SqlLab.IsReady())
            {
                return;
            }

            // Not a lesson method: it only builds and fills the bank tables the lesson queries.
            BankSeed.Recreate();

            var sqlBasics = new SqlBasicsDemo();

            sqlBasics.ExplainSelectAndWhere();
            sqlBasics.ExplainHandyFilters();
            sqlBasics.ExplainOrderingAndTop();
            sqlBasics.ExplainJoins();
            sqlBasics.ExplainGroupByAndAggregates();
            sqlBasics.ExplainWhereVsHaving();
        }
    }
}
