using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.ApiTesting
{
    // Adapts ApiTestingDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class ApiTestingExample : ILearningTopic
    {
        public void Explain()
        {
            var apiTesting = new ApiTestingDemo();

            apiTesting.ExplainWhatIsOpenApi();
            apiTesting.ExplainSwaggerUi();
            apiTesting.ExplainDocumentingEndpoints();
            apiTesting.ExplainPostmanAndHttpFiles();
            apiTesting.ExplainManualVsAutomatedTests();
        }
    }
}
