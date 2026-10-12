using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.HttpAndRest
{
    // Adapts HttpAndRestDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class HttpAndRestExample : ILearningTopic
    {
        public void Explain()
        {
            var httpAndRest = new HttpAndRestDemo();

            httpAndRest.ExplainRequestAndResponse();
            httpAndRest.ExplainHttpMethods();
            httpAndRest.ExplainStatusCodes();
            httpAndRest.ExplainJsonBodies();
            httpAndRest.ExplainRestResources();
        }
    }
}
