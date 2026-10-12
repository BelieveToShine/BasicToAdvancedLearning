using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.EfCoreCrud
{
    // Adapts EfCoreCrudDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class EfCoreCrudExample : ILearningTopic
    {
        public void Explain()
        {
            // This lesson needs SQL Server; SqlLab prints a friendly message and we stop if it is not set up.
            if (!SqlLab.IsReady())
            {
                return;
            }

            var efCrud = new EfCoreCrudDemo();

            efCrud.ExplainAddAndSave();
            efCrud.ExplainQueryingWithLinq();
            efCrud.ExplainRelatedData();
            efCrud.ExplainUpdatingTrackedEntities();
            efCrud.ExplainRemovingRows();
            efCrud.ExplainSaveChangesIsAllOrNothing();
        }
    }
}
