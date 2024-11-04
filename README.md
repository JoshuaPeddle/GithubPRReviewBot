# GithubPRReviewBot

** Still a work in progress **

## Overview

GitHub PR Review Bot is an automated review assistant designed to enhance code review workflows on GitHub. The bot listens for mentions in pull request (PR) comments (e.g., @pr_review_bot) and responds with a review, providing insights on code changes using AI.


### How It Works
GitHub Webhook Integration: The bot is set up as a GitHub App, which sends webhook events to this server when it’s mentioned in a PR. When a comment containing @pr_review_bot is detected, the bot springs into action.

Retrieving the Code Diff: The bot fetches the code changes (diff) for the PR from GitHub.

AI-Powered Review: The code diff is sent to a language model (LLM) via OpenAI’s API, which analyzes the changes and generates a review summary or suggestions.

Posting Feedback: The bot posts the AI-generated feedback directly as a comment on the PR, helping developers catch issues, gain insights, or improve code quality.

### Key Features
Automated Code Review: Provides AI-generated feedback on PRs when mentioned, streamlining the review process.
Seamless GitHub Integration: Works alongside GitHub’s API and is easily integrated as a GitHub App.
Configurable LLM Model: Supports configuration for various LLM models through OpenAI’s API.
This bot is ideal for teams looking to augment their code review process with AI-driven insights, making PR reviews faster and more informative.



## GitHub App Setup

### Permissions
![image](https://github.com/user-attachments/assets/c8e5667d-b3e1-415f-8377-6873cf4f9ab4)


## Build and push image
`docker buildx build -f .\GithubPRReviewBot\Dockerfile --platform linux/amd64,linux/arm64 -t <repo>/ghreviewbot:latest --push .`
