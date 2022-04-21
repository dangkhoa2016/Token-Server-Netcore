using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;

namespace TokenServerNetcore.Helpers
{
    public class TokenHelper
    {
        static string KEY_PREFIX = "netcore::access_token";
        List<string> SCOPES = new List<string>() {
          "https://www.googleapis.com/auth/drive.file",
          "https://www.googleapis.com/auth/drive.metadata",
          "https://www.googleapis.com/auth/drive.metadata.readonly",
          "https://www.googleapis.com/auth/drive.readonly",
          "https://www.googleapis.com/auth/drive"
        };
        AccountsHelper accounts_helper = new AccountsHelper();
        ReplitDatabase replit_database = new ReplitDatabase();
        Logger _log = new LoggerConfiguration()
                 .MinimumLevel.Debug()
                 .WriteTo.Console()
                 .CreateLogger();

        public TokenHelper()
        {
        }

        public async Task<JObject> GetAccountTokenInfo(string account_name, List<string> scope = null)
        {
            var account_json = await accounts_helper.GetAccount(account_name);
            if (account_json == null || account_json.HasValues == false)
            {
                _log.Information($"Can not get json for account: {account_name}");
                return null;
            }

            string s = JsonConvert.SerializeObject(account_json);
            return await ServiceAccountAuthorization(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(s)), scope);
        }

        public async Task<string> GetAccessToken(string account_name, List<string> scope = null)
        {
            var token_info = await replit_database.GetAsJson($"{KEY_PREFIX}_{account_name}");
            if (token_info != null && token_info.HasValues && token_info.ContainsKey("expired_time"))
            {
                _log.Information($"Get from Repl database for account: {account_name}");
                DateTime date = token_info.Value<DateTime>("expired_time");
                if (date.AddSeconds(-50) > DateTime.Now.ToUniversalTime())
                    return token_info.Value<string>("access_token");
            }

            token_info = await GetAccountTokenInfo(account_name, scope);
            if (token_info != null && token_info.HasValues)
            {
                var json = JObject.FromObject(new
                {
                    access_token = Regex.Replace(token_info.Value<string>("access_token"), "[.]+$", ""),
                    expired_time = token_info.Value<DateTime>("expires_at")
                });

                _log.Information($"Save token for account: {account_name}");
                await replit_database.Put($"{KEY_PREFIX}_{account_name}", json);

                return json.Value<string>("access_token");
            }

            return string.Empty;
        }

        public async Task<string> GetRandomToken(List<string> scope = null)
        {
            var account = await accounts_helper.GetRandomAccount();
            return await GetAccessToken(account, scope);
        }

        public async Task<JObject> ServiceAccountAuthorization(Stream credentials_json, List<string> scope = null)
        {
            if (scope == null || scope.Count == 0)
                scope = SCOPES;

            try
            {
                var credential = GoogleCredential.FromStream(credentials_json).CreateScoped(scope)
                                .UnderlyingCredential as ServiceAccountCredential;
                await credential.GetAccessTokenForRequestAsync();

                return JObject.FromObject(new
                {
                    scope = scope,
                    access_token = credential.Token.AccessToken,
                    expiry = 60,
                    issued_at = credential.Token.IssuedUtc,
                    expires_at = DateTime.Now.ToUniversalTime().AddSeconds(credential.Token.ExpiresInSeconds.Value),
                    access_type = "offline",
                    refresh_token = "",
                    token_type = credential.Token.TokenType,
                    grant_type = "urn:ietf:params:oauth:grant-type:jwt-bearer"
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error gtoken getToken");
            }

            return null;
        }
    }
}
