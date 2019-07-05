using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Kugar.Tools.SMS.Demo.Controllers
{
    [Route("test/{action}")]
    public class TestController : Controller
    {
        private IConfiguration _config = null;

        public TestController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            var id = _config["Ali:accessKeyId"];
            var key = _config["Ali:accessKeySecret"];
            var smsTempate = _config["Ali:SMS_Template"];
            var signName = _config["Ali:signature"];
            var mobile = _config["TestMobile"];

            var s=new AliyunSmsSender(id, key);
            var ret=await s.SendAsync(mobile/*"13750467409"*/, signName/* "BTC官网"*/, smsTempate/*"SMS_152856017"*/, new Dictionary<string, string>()
            {
                ["code"] = "569822"
            });

            return View();
        }
    }
}