using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace TokenServerNetcore.Helpers
{
    public class AccountsHelper
    {
        MemoryCache cacheHelper = new MemoryCache(new MemoryCacheOptions());
        string cacheKey = "AccountsHelper-account_names";
        static string BASE_URL = Environment.GetEnvironmentVariable("BASE_URL");
        HttpClient client = null;

        TimeSpan expireTime = TimeSpan.FromDays(1);
        Logger _log = new LoggerConfiguration()
                 .MinimumLevel.Debug()
                 .WriteTo.Console()
                 .CreateLogger();

        public AccountsHelper()
        {
            client = new HttpClient() { BaseAddress = new Uri($"{BASE_URL}/accounts/") };
            client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("TokenServer-Netcore", "1.0"));
        }

        public async Task<JObject> GetAccount(string accountName)
        {
            try
            {
                var response = await client.GetAsync(accountName);
                if (response != null && response.IsSuccessStatusCode)
                    return JObject.Parse(await response.Content.ReadAsStringAsync());
                else
                    LogError(response);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error GetAccount");
            }

            return null;
        }

        public async Task<List<string>> AccountNames()
        {
            var value = cacheHelper.Get<List<string>>(cacheKey);
            if (value == null || value.Count == 0)
            {
                _log.Information("account_names key expires");
                value = await GetAccounts();
                cacheHelper.Set(cacheKey, value, expireTime);
            }
            return value;
        }

        public async Task<string> GetRandomAccount()
        {
            var accounts = await AccountNames();
            if (accounts != null && accounts.Count > 0)
                return accounts[new Random().Next(0, accounts.Count)];

            return null;
        }

        public async Task<JObject> GetRandomJsonAccount()
        {
            var account = await GetRandomAccount();
            return await GetAccount(account);
        }

        async void LogError(HttpResponseMessage response)
        {
            string content = response.Content != null ? (await response.Content.ReadAsStringAsync()) : string.Empty;
            _log.Information($"{response.StatusCode}, {content}");
        }

        async Task<List<string>> GetAccounts()
        {
            try
            {
                var response = await client.GetAsync("list");
                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<string>>(content);
                }
                else
                    LogError(response);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error GetAccounts");
            }

            return null;
        }

    }
}
