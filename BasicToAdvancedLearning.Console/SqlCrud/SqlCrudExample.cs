using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.SqlCrud
{
    // Adapts SqlCrudDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class SqlCrudExample : ILearningTopic
    {
        public void Explain()
        {
            // This lesson talks to SQL Server: stop with a friendly message if no connection string is set.
            if (!SqlLab.IsReady())
            {
                return;
            }

            SqlCrudSetup.CreateBankTables();

            var sqlCrud = new SqlCrudDemo();

            sqlCrud.ExplainInsertRows();
            sqlCrud.ExplainUpdateRows();
            sqlCrud.ExplainDeleteRows();
            sqlCrud.ExplainTransactions();
            sqlCrud.ExplainXactAbort();
            sqlCrud.ExplainSafeHabits();
        }
    }
}
