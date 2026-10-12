using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.WebApiBasics
{
    // Adapts WebApiBasicsDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class WebApiBasicsExample : ILearningTopic
    {
        public void Explain()
        {
            var webApiBasics = new WebApiBasicsDemo();

            webApiBasics.ExplainControllerAndAction();
            webApiBasics.ExplainRouting();
            webApiBasics.ExplainModelBinding();
            webApiBasics.ExplainActionResults();
            webApiBasics.ExplainDependencyInjection();
            webApiBasics.ExplainServiceLifetimes();
        }
    }
}
