using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.ExceptionHandling
{
    // Adapts ExceptionHandlingDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class ExceptionHandlingExample : ILearningTopic
    {
        public void Explain()
        {
            var exceptionHandling = new ExceptionHandlingDemo();

            exceptionHandling.ExplainTryCatch();
            exceptionHandling.ExplainCatchOrder();
            exceptionHandling.ExplainFinally();
            exceptionHandling.ExplainCustomExceptions();
        }
    }
}
