# GithubPRReviewBot

## Build and push image
`docker buildx build -f .\GithubPRReviewBot\Dockerfile --platform linux/amd64,linux/arm64 -t <repo>/ghreviewbot:latest --push .`
