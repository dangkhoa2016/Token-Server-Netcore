using System.Net.Mime;
using DynamicExpresso;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using TokenServerNetcore.Helpers;

namespace TokenServerNetcore.Controllers
{
    [Route("/")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        static Interpreter interpreter = new Interpreter().EnableReflection()
            .SetVariable("accounts_helper", new AccountsHelper())
            .SetVariable("token_helper", new TokenHelper())
            .SetVariable("replit_database", new ReplitDatabase());

        readonly IHostEnvironment _hostingEnvironment;
        private readonly ILogger<HomeController> _logger;
        public HomeController(ILogger<HomeController> logger, IHostEnvironment hostingEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet("/")]
        public IActionResult Welcome()
        {
            return new JsonResult(new { hello = "world" });
        }


        static bool IsPropertyExist(dynamic obj, string name)
        {
            if (obj is ExpandoObject)
                return ((IDictionary<string, object>)obj).ContainsKey(name);

            return obj.GetType().GetProperty(name) != null;
        }

        [HttpPost("eval")]
        public IActionResult Eval([FromForm] string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new JsonResult(new { error = "Please provide code." });

            Func<dynamic, ContentResult> response = (value) =>
            {
                var responseData = JsonConvert.SerializeObject(value);
                _logger.LogInformation($"Eval code: {responseData}");
                return Content(responseData, MediaTypeNames.Text.Plain);
            };

            dynamic result = interpreter.Parse(content).Invoke();
            if (IsPropertyExist(result, "Result"))
                return response(result.Result);
            else
                return response(result);
        }
    }
}