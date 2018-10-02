# K8s Blue/Green deployment strategy:
Blue/Green strategy is used to minimize downtime for production services. 

Its typically achieved by keeping two identical production environments - **Prod**(**Green**) and **Stage**(**Blue**). Only one of which is serving live production traffic. 

When a new release has to be rolled out, its first deployed in the **Stage** environment. When all the testing is done, the live traffic is moved to **Stage** which becomes the **Prod** environment, and current **Prod** environment becomes the **Stage**. There is added benefit of a fast rollback by just changing route if new issue is found with live traffic.


# Version App

## Creating WebApi
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
Version Controller will just return his current version for Get requests at http://localhost:{port}/api/version.
We will start from pre-release version `0.5`.

Now our API is developed, we could check how it’s work:
```powershell
> dotnet run --project .\Checker\
Hosting environment: Production
Content root path: .\Checker
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```
After starting we could check it at http://localhost:5000/api/version.

Now we could shut down it and add Docker Support for it.

## Docker Support

For supporting docker we should add three files:
* `dockerfile` - build instructions to build the image
* `docker-compose.yml` - configure your application’s services
* `.dockerignore` - increase the build’s performance by excluding unneeded files and directories

Really `docker-compose.yml` is not needed because it's more for multi-container docker applications:
>Compose is a tool for defining and running multi-container Docker applications. With Compose, you use a YAML file to configure your application’s services. Then, with a single command, you create and start all the services from your configuration.

Here is default `.dockerignore` file:
```docker
.dockerignore
.env
.git
.gitignore
.vs
.vscode
docker-compose.yml
docker-compose.*.yml
*/bin
*/obj
```
And instructions how to build our image for `dockerfile` file:
```docker
FROM microsoft/dotnet:2.2-sdk AS build-env
WORKDIR /app
EXPOSE 5000
EXPOSE 80

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Checker.dll"]
```
And `docker-compose.yml` file:
```docker
version: '3.4'

services:
  version-api:
    image: bbenetskyy/version-api:v0.5
    ports:
      - "5000:80"
    build:
      context: .
      dockerfile: dockerfile
```
In this file we will change our image version.

And how we could start a docker container based on our Version Api image with version `v0.5`.
```powershell
> docker-compose build;
> docker-compose up;
```
> We could also use `docker run bbenetskyy/version-api:v0.5` to start image

## Kubernetes Support
