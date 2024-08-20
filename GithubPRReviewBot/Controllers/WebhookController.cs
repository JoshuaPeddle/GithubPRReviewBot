using Microsoft.AspNetCore.Mvc;
using Octokit;
using OpenAI_API;
using System.Net.Http.Headers;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using GithubPRReviewBot.Models;
using GithubPRReviewBot;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private GitHubClient _githubClient;
    private const string privateKey = "C:\\Users\\joshu\\Downloads\\pr-review-llm.2024-08-19.private-key.pem";
    private const int appId = 974247;


    public WebhookController()
    {


        // Initialize GitHub client
       
    }

    [HttpPost]
    public async Task<IActionResult> Handle([FromBody] JsonElement payload)
    {// Safely extract values from the JsonElement

        var jwtToken = GitHubAppAuth.GenerateJwt(privateKey, appId);

        var appClient = new GitHubClient(new Octokit.ProductHeaderValue("PRReviewBot"))
        {
            Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
        };

        var installations = await appClient.GitHubApps.GetAllInstallationsForCurrent();
        var installationId = installations[0].Id;

        var installationToken = await appClient.GitHubApps.CreateInstallationToken(installationId);

        _githubClient = new GitHubClient(new Octokit.ProductHeaderValue("PRReviewBot"))
        {
            Credentials = new Credentials(installationToken.Token)
        };


        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var webhookPayload = JsonSerializer.Deserialize<GitHubWebhookPayload>(payload.GetRawText(), options);

        if (webhookPayload == null)
        {
            return BadRequest(new { error = "Invalid payload." });
        }

        var action = webhookPayload.Action;
        var commentBody = webhookPayload.Comment?.Body;
        var repositoryOwner = webhookPayload.Repository?.Owner?.Login;
        var repositoryName = webhookPayload.Repository?.Name;
        var issueNumber = webhookPayload.Issue?.Number;


        // Check if the bot is mentioned
        if (action == "created" && commentBody.Contains("@pr_review_bot"))
        {
            // Get the pull request diff
            var pullRequest = await _githubClient.PullRequest.Get(repositoryOwner, repositoryName, issueNumber.Value);

            var diffUrl = pullRequest.DiffUrl;

            // Download the diff

            var diff = await GetPullRequestDiffUsingApi(repositoryOwner, repositoryName, issueNumber.Value);




            // Send the diff to an LLM (e.g., OpenAI GPT)
            var openAI = new OpenAIAPI("YOUR_OPENAI_API_KEY");
            openAI.ApiUrlFormat = "https://api.openai.com/{0}/{1}";
            //var completionRequest = new OpenAI_API.Completions.CompletionRequest
            //{
            //    Model = "text-davinci-003",
            //    Prompt = $"Please review the following code diff: {diff}",
            //    MaxTokens = 500
            //};
            //var completion = await openAI.Completions.CreateCompletionAsync(completionRequest);

            //var responseContent = completion.Choices[0].Text;

            //// Post a comment back to the PR
            //var comment = new IssueCommentUpdate(responseContent);
            //await _githubClient.Issue.Comment.Create(repositoryOwner, repositoryName, issueNumber, comment);

            return Ok();
        }

        return BadRequest();
    }

    public async Task<string> GetPullRequestDiffUsingApi(string repositoryOwner, string repositoryName, int pullRequestNumber)
    {
        using var httpClient = new HttpClient();

        // Set the Authorization header with the installation token
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _githubClient.Credentials.GetToken());

        // Set the Accept header to request the diff format
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.diff"));

        // Add the User-Agent header
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PRReviewBot", "1.0"));

        // Make the request to the GitHub API
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