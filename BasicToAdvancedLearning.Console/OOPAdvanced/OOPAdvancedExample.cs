using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.OOPAdvanced
{
    // Adapts OOPAdvancedDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class OOPAdvancedExample : ILearningTopic
    {
        public void Explain()
        {
            var oopAdvanced = new OOPAdvancedDemo();

            oopAdvanced.ExplainInheritance();
            oopAdvanced.ExplainPolymorphism();
            oopAdvanced.ExplainAbstractClasses();
            oopAdvanced.ExplainInterfaces();
        }
    }
}
