using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.WebApiCrud
{
    // Adapts WebApiCrudDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class WebApiCrudExample : ILearningTopic
    {
        public void Explain()
        {
            // This lesson needs a SQL Server; without a connection string it prints why and stops.
            if (!SqlLab.IsReady())
            {
                return;
            }

            // `using` stops the in-process web server when the lesson ends.
            using var webApiCrud = new WebApiCrudDemo();

            webApiCrud.ExplainDbContextInDI();
            webApiCrud.ExplainReadEndpoints();
            webApiCrud.ExplainCreateEndpoint();
            webApiCrud.ExplainUpdateAndDelete();
        }
    }
}
