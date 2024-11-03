using OpenAI_API.Chat;
using OpenAI_API;

namespace GithubPRReviewBot.Services
{
    public class LlmReviewService
    {
        private readonly IConfiguration _configuration;
        public LlmReviewService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<ChatResult> LlmReviewDiff(string diff)
        {
            var apiKey = _configuration["OpenAi:ApiKey"];
            var model = _configuration["OpenAi:Model"];

            var openAI = new OpenAIAPI(apiKey);

            var chatRequest = new ChatRequest()
            {
                Messages =
                [
                    new ChatMessage(ChatMessageRole.User, $"Please review the following code diff: {diff}")
                ],
                Model = model,
            };

            var result = await openAI.Chat.CreateChatCompletionAsync(chatRequest);
            return result;
        }
    }
}
