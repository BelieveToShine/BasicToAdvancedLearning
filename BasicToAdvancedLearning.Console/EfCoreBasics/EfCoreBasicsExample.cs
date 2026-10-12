using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.EfCoreBasics
{
    // Adapts EfCoreBasicsDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on - same pattern as every other topic.
    internal class EfCoreBasicsExample : ILearningTopic
    {
        public void Explain()
        {
            // Every method after the first talks to SQL Server; skip politely when it is not set up.
            if (!SqlLab.IsReady())
            {
                return;
            }

            var efCoreBasics = new EfCoreBasicsDemo();

            efCoreBasics.ExplainModelClasses();
            efCoreBasics.ExplainDbContextAndDbSet();
            efCoreBasics.ExplainFluentApiConfiguration();
            efCoreBasics.ExplainMigrations();
            efCoreBasics.ExplainEnsureCreatedVsMigrate();
        }
    }
}
