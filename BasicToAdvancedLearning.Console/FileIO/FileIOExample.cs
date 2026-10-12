using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.FileIO
{
    // Adapts FileIODemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on — same pattern as every other topic.
    internal class FileIOExample : ILearningTopic
    {
        public void Explain()
        {
            var fileIO = new FileIODemo();

            fileIO.ExplainWriteAndReadText();
            fileIO.ExplainLinesAndStreams();
            fileIO.ExplainPathsAndExistence();
            fileIO.ExplainFileErrors();
        }
    }
}
