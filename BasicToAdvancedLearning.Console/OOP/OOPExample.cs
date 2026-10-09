using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.OOP
{
    // Adapts OOPDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class OOPExample : ILearningTopic
    {
        public void Explain()
        {
            var oop = new OOPDemo();

            oop.ExplainClassesAndObjects();
            oop.ExplainConstructors();
            oop.ExplainEncapsulation();
            oop.ExplainReferencesVsCopies();
            oop.ExplainStaticMembers();
        }
    }
}
