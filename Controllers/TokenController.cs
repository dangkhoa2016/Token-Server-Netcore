using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Mime;
using System.Linq;
using System.IO;
using TokenServerNetcore.Helpers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Net;

namespace TokenServerNetcore.Controllers
{
    [ApiController]
    [Route("/token")]
    public class TokenController : ControllerBase
    {
        readonly ILogger<TokenController> _logger;
        AccountsHelper accounts_helper = new AccountsHelper();
        TokenHelper token_helper = new TokenHelper();

        public TokenController(ILogger<TokenController> logger)
        {
            _logger = logger;
        }

        [HttpGet("random")]
        public async Task<IActionResult> random([FromQuery] string scope = "")
        {
            try
            {
                List<string> lstScope = null;
                if (string.IsNullOrWhiteSpace(scope) == false)
                    lstScope = scope.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();

                var token = await token_helper.GetRandomToken(lstScope);
                return new ContentResult() { Content = token };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error get random token");
                return new ContentResult()
                {
                    Content = JsonConvert.SerializeObject(new { error = ex.Message }),
                    ContentType = MediaTypeNames.Application.Json,
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }

        [Route("{*account}")]
        [HttpGet]
        public async Task<IActionResult> token([FromRoute] string account, [FromQuery] string type = "", [FromQuery] string scope = "")
        {
            try
            {
                return await ProcessRequest(account, type, scope);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ProcessRequest");
                return new ContentResult()
                {
                    Content = JsonConvert.SerializeObject(new { error = ex.Message }),
                    ContentType = MediaTypeNames.Application.Json,
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }

        async Task<IActionResult> ProcessRequest(string account, string type = "", string scope = "")
        {
            List<string> lstScope = null;
            if (string.IsNullOrWhiteSpace(scope) == false)
                lstScope = scope.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();

            if (string.IsNullOrWhiteSpace(type))
                type = "access_token";
            type = type.ToLower();
            if (type == "account_json")
            {
                JObject result = await accounts_helper.GetAccount(account);
                if (result != null && result.HasValues)
                    return Content(JsonConvert.SerializeObject(result), MediaTypeNames.Application.Json);
            }
            else if (type == "token_info")
            {
                var token_info = await token_helper.GetAccountTokenInfo(account, lstScope);
                if (token_info != null && token_info.HasValues)
                {
                    return Content(JsonConvert.SerializeObject(new
                    {
                        expires_at = token_info.Value<DateTime>("expires_at"),
                        issued_at = token_info.Value<DateTime>("issued_at"),
                        access_type = token_info.Value<string>("access_type"),
                        expiry = token_info.Value<int>("expiry"),
                        access_token = token_info.Value<string>("access_token"),
                        refresh_token = token_info.Value<string>("refresh_token"),
                        grant_type = token_info.Value<string>("grant_type"),
                        scope = token_info.Value<JArray>("scope"),
                    }), MediaTypeNames.Application.Json);
                }
            }
            else
            {
                var token = await token_helper.GetAccessToken(account, lstScope);
                if (string.IsNullOrWhiteSpace(token) == false)
                    return new ContentResult() { Content = token };
            }

            return Content(JsonConvert.SerializeObject(new { error = $"Account: {account} does not exists." }), MediaTypeNames.Application.Json);
        }

    }
}
