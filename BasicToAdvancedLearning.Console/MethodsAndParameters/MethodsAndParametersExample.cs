using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.MethodsAndParameters
{
    // Adapts MethodsAndParametersDemo (the actual lesson code) to the ILearningTopic
    // contract Program.cs depends on — same pattern as every other topic.
    internal class MethodsAndParametersExample : ILearningTopic
    {
        public void Explain()
        {
            var methodsAndParameters = new MethodsAndParametersDemo();

            methodsAndParameters.ExplainMethodBasics();
            methodsAndParameters.ExplainParameters();
            methodsAndParameters.ExplainOptionalAndNamedParameters();
            methodsAndParameters.ExplainParamsKeyword();
            methodsAndParameters.ExplainMethodOverloading();
        }
    }
}
