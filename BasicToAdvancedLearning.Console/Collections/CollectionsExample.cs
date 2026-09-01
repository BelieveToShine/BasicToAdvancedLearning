using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.Collections
{
    // Adapts CollectionsDemo (the actual lesson code) to the ILearningTopic
    // contract Program.cs depends on — same pattern as every other topic.
    internal class CollectionsExample : ILearningTopic
    {
        public void Explain()
        {
            var collections = new CollectionsDemo();

            collections.ExplainArrays();
            collections.ExplainLists();
            collections.ExplainDictionaries();
            collections.ExplainCollectionSafety();
            collections.ExplainChoosingACollection();
        }
    }
}
