using Octokit;
using OpenAI_API.Chat;
using System.Net.Http.Headers;

namespace GithubPRReviewBot.Services
{
    public class GitHubWebhookService
    {
        private GitHubClient? _githubClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly int _appId;

        public GitHubWebhookService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _appId = int.Parse(configuration["GitHub:AppId"]);

            var privateKeyPath = configuration["GitHub:PrivateKeyPath"];
            if (string.IsNullOrEmpty(privateKeyPath))
                throw new ArgumentException("GitHub:PrivateKeyPath is required.");
            if (!File.Exists(privateKeyPath))
                throw new ArgumentException("GitHub:PrivateKeyPath does not exist.");

            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task CreateComment(string repositoryOwner, string repositoryName, int issueNumber, ChatResult result)
        {
            if (_githubClient == null)
                await SetupClient();

            await _githubClient!.Issue.Comment.Create(repositoryOwner, repositoryName, issueNumber, result.Choices[0].Message.TextContent);
        }

        public async Task<string> GetPullRequestDiff(string repositoryOwner, string repositoryName, int pullRequestNumber)
        {
            if (_githubClient == null)
                await SetupClient();
            using var httpClient = _httpClientFactory.CreateClient();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _githubClient!.Credentials.GetToken());
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.diff"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PRReviewBot", "1.0"));

            var requestUrl = $"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/pulls/{pullRequestNumber}";

            var diff = await httpClient.GetStringAsync(requestUrl);
            return diff;
        }

        private async Task SetupClient()
        {
            var jwtToken = GitHubAppAuth.GenerateJwt(_configuration["GitHub:PrivateKeyPath"], _appId);

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
        }
    }
}
