using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace VersionApi.Controllers
{
    [Route("/")]
    public class VersionController : Controller
    {
        [HttpGet]
        public string Get() => "v2.0";
    }
}
