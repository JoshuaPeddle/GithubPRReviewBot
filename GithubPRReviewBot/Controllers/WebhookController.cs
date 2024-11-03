using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using GithubPRReviewBot.Models;
using System.Text.Json.Nodes;
using GithubPRReviewBot.Services;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly LlmReviewService _llmReviewService;
    private readonly GitHubWebhookService _gitHubWebhookService;
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WebhookController(LlmReviewService llmReviewService, GitHubWebhookService gitHubWebhookService)
    {
        _llmReviewService = llmReviewService;
        _gitHubWebhookService = gitHubWebhookService;
    }

    [HttpPost]
    public async Task<IActionResult> Handle([FromBody] JsonElement payload)
    {
        var node = JsonNode.Parse(payload.GetRawText());

        if (node == null)
            return BadRequest(new { error = "Invalid webhook payload." });

        var webhookPayload = node["payload"].Deserialize<GitHubWebhookPayload>(serializerOptions);

        if (webhookPayload == null)
            return BadRequest(new { error = "Invalid webhook payload." });

        var action = webhookPayload.Action;
        var commentBody = webhookPayload.Comment.Body;

        if (action == "created" && commentBody.Contains("@pr_review_bot"))
        {
            var repositoryOwner = webhookPayload.Repository.Owner.Login;
            var repositoryName = webhookPayload.Repository.Name;
            var issueNumber = webhookPayload.Issue.Number;

            var diff = await _gitHubWebhookService.GetPullRequestDiff(repositoryOwner, repositoryName, issueNumber);

            var result = await _llmReviewService.LlmReviewDiff(diff);

            await _gitHubWebhookService.CreateComment(repositoryOwner, repositoryName, issueNumber, result);

            return Ok();
        }

        return BadRequest();
    }
}