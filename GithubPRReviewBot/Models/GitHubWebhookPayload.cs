namespace GithubPRReviewBot.Models
{
    public class GitHubWebhookPayload
    {
        public string Action { get; set; }
        public Issue Issue { get; set; }
        public Comment Comment { get; set; }
        public Repository Repository { get; set; }
    }

    public class Issue
    {
        public int Number { get; set; }
    }

    public class Comment
    {
        public string Body { get; set; }
    }

    public class Repository
    {
        public string Name { get; set; }
        public Owner Owner { get; set; }
    }

    public class Owner
    {
        public string Login { get; set; }
    }
}
