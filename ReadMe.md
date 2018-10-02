# K8s Blue/Green deployment strategy:
Blue/Green strategy is used to minimize downtime for production services. 

Its typically achieved by keeping two identical production environments - **Prod**(**Green**) and **Stage**(**Blue**). Only one of which is serving live production traffic. 

When a new release has to be rolled out, its first deployed in the **Stage** environment. When all the testing is done, the live traffic is moved to **Stage** which becomes the **Prod** environment, and current **Prod** environment becomes the **Stage**. There is added benefit of a fast rollback by just changing route if new issue is found with live traffic.


# Version App

For testing Blue/Green Deployment we will create simple API which will return just his current version.

Let's start from creating .Net Core WebApi project names `Checker`
```powershell
> dotnet new webapi -n Checker
The template "ASP.NET Core Web API" was created successfully.
This template contains technologies from parties other than Microsoft, see https://aka.ms/template-3pn for details.

Processing post-creation actions...
Running 'dotnet restore' on Checker\Checker.csproj...
  Restoring packages for .\Checker\Checker.csproj...
  Restore completed in 66.95 ms for .\Checker\Checker.csproj.
  Generating MSBuild file .\Checker\obj\Checker.csproj.nuget.g.props.
  Generating MSBuild file .\Checker\obj\Checker.csproj.nuget.g.targets.
  Restore completed in 1.96 sec for .\Checker\Checker.csproj.

Restore succeeded.
```
Remove default `ValuesController` and add our simple `VersionController`:
```powershell
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Checker.Controllers
{
    [Route("api/[controller]")]
    public class VersionController : Controller
    {
        [HttpGet]
        public string Get() => "v0.5";
    }
}
```
Version Controller will just return his current version for Get requests  at http://localhost:{port}/api/version.
We will start from pre-release version `0.5`.

