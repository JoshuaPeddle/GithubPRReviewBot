using GithubPRReviewBot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddHttpClient();

builder.Services.AddScoped<LlmReviewService>();
builder.Services.AddScoped<GitHubWebhookService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();
