using Octokit;
using Octokit.Internal;
using System;
using System.Security.Cryptography;
using System.Text;
using Jose; // Install JWT library via NuGet (dotnet add package jose-jwt)

namespace GithubPRReviewBot
{

    public class GitHubAppAuth
    {
        public static string GenerateJwt(string pemFilePath, int appId)
        {
            var privateKeyRsa = LoadRsaPrivateKeyFromPem(pemFilePath);

            var utcNow = DateTime.UtcNow;

            var payload = new
            {
                iat = ToUnixTime(utcNow),  // Issued at time
                exp = ToUnixTime(utcNow.AddMinutes(10)),  // Expiration time (10 minutes is recommended)
                iss = appId  // GitHub App ID
            };

            return Jose.JWT.Encode(payload, privateKeyRsa, JwsAlgorithm.RS256);
        }

        private static RSA LoadRsaPrivateKeyFromPem(string pemFilePath)
        {
            var pem = File.ReadAllText(pemFilePath);

            // Remove the header and footer
            var base64 = pem
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();

            var keyBytes = Convert.FromBase64String(base64);

            var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(keyBytes, out _);
            return rsa;
        }

        private static long ToUnixTime(DateTime dateTime)
        {
            return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        }
    }
}
