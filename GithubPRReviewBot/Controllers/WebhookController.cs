using Microsoft.AspNetCore.Mvc;
using Octokit;
using OpenAI_API;
using System.Net.Http.Headers;
using System.Text.Json;
using GithubPRReviewBot.Models;
using GithubPRReviewBot;
using System.Text.Json.Nodes;
using OpenAI_API.Chat;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private GitHubClient _githubClient;
    private const string privateKey = "C:\\Users\\josh\\Downloads\\pr-review-llm.2024-11-01.private-key.pem";
    private const int appId = 974247;

    public WebhookController()
    {
        var jwtToken = GitHubAppAuth.GenerateJwt(privateKey, appId);

        var appClient = new GitHubClient(new Octokit.ProductHeaderValue("PRReviewBot"))
        {
            Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
        };

        var installations = appClient.GitHubApps.GetAllInstallationsForCurrent().Result;
        var installationId = installations[0].Id;

        var installationToken = appClient.GitHubApps.CreateInstallationToken(installationId).Result;

        _githubClient = new GitHubClient(new Octokit.ProductHeaderValue("PRReviewBot"))
        {
            Credentials = new Credentials(installationToken.Token)
        };
    }

    [HttpPost]
    public async Task<IActionResult> Handle([FromBody] JsonElement payload)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var node = JsonNode.Parse(payload.GetRawText());
        var webhookPayload = node["payload"].Deserialize<GitHubWebhookPayload>(options);


        if (webhookPayload == null)
        {
            return BadRequest(new { error = "Invalid payload." });
        }

        var action = webhookPayload.Action;
        var commentBody = webhookPayload.Comment.Body;
        var repositoryOwner = webhookPayload.Repository.Owner.Login;
        var repositoryName = webhookPayload.Repository.Name;
        var issueNumber = webhookPayload.Issue.Number;

        if (action == "created" && commentBody.Contains("@pr_review_bot"))
        {
            var pullRequest = await _githubClient.PullRequest.Get(repositoryOwner, repositoryName, issueNumber);

            var diffUrl = pullRequest.DiffUrl;

            var diff = await GetPullRequestDiffUsingApi(repositoryOwner, repositoryName, issueNumber);

            ChatResult result = await LlmReviewDiff(diff);

            await _githubClient.Issue.Comment.Create(repositoryOwner, repositoryName, issueNumber, result.Choices[0].Message.TextContent);

            return Ok();
        }

        return BadRequest();
    }

    private static async Task<ChatResult> LlmReviewDiff(string diff)
    {
        var openAI = new OpenAIAPI("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");

        var chatRequest = new OpenAI_API.Chat.ChatRequest()
        {
            Messages = new List<ChatMessage> { new ChatMessage(ChatMessageRole.User, $"Please review the following code diff: {diff}") },
            Model = "gpt-4o-mini",
        };

        var result = await openAI.Chat.CreateChatCompletionAsync(chatRequest);
        return result;
    }

    private async Task<string> GetPullRequestDiffUsingApi(string repositoryOwner, string repositoryName, int pullRequestNumber)
    {
        using var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _githubClient.Credentials.GetToken());
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.diff"));
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PRReviewBot", "1.0"));

        var requestUrl = $"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/pulls/{pullRequestNumber}";

        try
        {
            var diff = await httpClient.GetStringAsync(requestUrl);
            return diff;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error fetching diff: {ex.Message}");
            throw;
        }
    }
}