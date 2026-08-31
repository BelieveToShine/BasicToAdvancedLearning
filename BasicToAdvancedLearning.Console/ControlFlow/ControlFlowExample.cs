using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.ControlFlow
{
    // Adapts ControlFlowDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as ProgrammingBasicsExample.
    internal class ControlFlowExample : ILearningTopic
    {
        public void Explain()
        {
            var controlFlow = new ControlFlowDemo();

            controlFlow.ExplainIfElse();
            controlFlow.ExplainSwitch();
            controlFlow.ExplainForLoop();
            controlFlow.ExplainWhileLoop();
            controlFlow.ExplainDoWhileLoop();
            controlFlow.ExplainForeach();
        }
    }
}
