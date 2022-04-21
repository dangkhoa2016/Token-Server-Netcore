using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TokenServerNetcore.Helpers
{
    public class ReplitDatabase
    {
        Logger _log = new LoggerConfiguration()
                  .MinimumLevel.Debug()
                  .WriteTo.Console()
                  .CreateLogger();
        static string REPLIT_DB_URL = Environment.GetEnvironmentVariable("REPLIT_DB_URL");
        readonly HttpClient client = new HttpClient();

        public ReplitDatabase()
        {
        }

        public async Task<bool> Put(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            value = value != null ? value : "";
            value = value.GetType() == typeof(string) ? value : Newtonsoft.Json.JsonConvert.SerializeObject(value);
            var dict = new Dictionary<string, string>();
            dict.Add(key, value.ToString());

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, REPLIT_DB_URL) { Content = new FormUrlEncodedContent(dict) };
                var res = await client.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _log.Error("Error Put", ex);
            }

            return false;
        }

        public async Task<HttpContent> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            try
            {
                var response = await client.GetAsync($"{REPLIT_DB_URL}/{Uri.EscapeDataString(key)}");
                if (response != null && response.IsSuccessStatusCode)
                    return response.Content;
                else
                    LogError(response);
            }
            catch (Exception ex)
            {
                _log.Error("Error Get", ex);
            }

            return null;
        }

        public async Task<string> GetBody(string key)
        {
            try
            {
                var content = await Get(key);
                return content != null ? (await content.ReadAsStringAsync()) : null;
            }
            catch (Exception ex)
            {
                _log.Error("Error GetBody", ex);
            }

            return null;
        }

        public async Task<JObject> GetAsJson(string key)
        {
            try
            {
                var body = await GetBody(key);
                return string.IsNullOrWhiteSpace(body) ? null : JObject.Parse(body);
            }
            catch (Exception ex)
            {
                _log.Error("Error GetAsJson", ex);
            }

            return null;
        }

        public async Task<bool> Delete(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                var response = await client.DeleteAsync($"{REPLIT_DB_URL}/{Uri.EscapeDataString(key)}");
                if (response != null && response.IsSuccessStatusCode)
                    return true;
                else
                    LogError(response);
            }
            catch (Exception ex)
            {
                _log.Error("Error Delete", ex);
            }

            return false;
        }

        public async Task<List<string>> List(string prefix = "")
        {
            try
            {
                var response = await client.GetAsync($"{REPLIT_DB_URL}?prefix={Uri.EscapeDataString(prefix)}");
                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return content.Split(Environment.NewLine.ToCharArray()).Where(z => string.IsNullOrWhiteSpace(z) == false).ToList();
                }
                else
                    LogError(response);
            }
            catch (Exception ex)
            {
                _log.Error("Error List", ex);
            }

            return null;
        }
        async void LogError(HttpResponseMessage response)
        {
            string content = response.Content != null ? (await response.Content.ReadAsStringAsync()) : string.Empty;
            _log.Information($"{response.StatusCode}, {content}");
        }

    }
}
