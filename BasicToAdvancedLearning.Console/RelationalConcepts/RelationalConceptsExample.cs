using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.RelationalConcepts
{
    // Adapts RelationalConceptsDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class RelationalConceptsExample : ILearningTopic
    {
        public void Explain()
        {
            // This lesson needs a real SQL Server. IsReady() prints a friendly message and returns
            // false when the connection string is missing or the server cannot be reached.
            if (!SqlLab.IsReady())
            {
                return;
            }

            var relational = new RelationalConceptsDemo();

            relational.ExplainTablesRowsColumns();
            relational.ExplainPrimaryKeys();
            relational.ExplainForeignKeys();
            relational.ExplainConstraintsAndNulls();
            relational.ExplainNormalization();
        }
    }
}
