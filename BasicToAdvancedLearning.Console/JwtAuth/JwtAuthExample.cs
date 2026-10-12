using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.JwtAuth
{
    // Adapts JwtAuthDemo (the actual lesson code) to the ILearningTopic contract
    // Program.cs depends on - same pattern as every other topic.
    internal class JwtAuthExample : ILearningTopic
    {
        public void Explain()
        {
            var jwtAuth = new JwtAuthDemo();

            jwtAuth.ExplainAuthenticationVsAuthorization();
            jwtAuth.ExplainWhatIsAJwt();
            jwtAuth.ExplainIssuingATokenLogin();
            jwtAuth.ExplainProtectingEndpoints();
            jwtAuth.ExplainTokenLifetime();
        }
    }
}
