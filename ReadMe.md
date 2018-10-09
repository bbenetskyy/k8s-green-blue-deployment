# K8s Blue/Green deployment strategy:
Blue/Green strategy is used to minimize downtime for production services. 

Its typically achieved by keeping two identical production environments - **Prod**(**Green**) and **Stage**(**Blue**). Only one of which is serving live production traffic. 

When a new release has to be rolled out, its first deployed in the **Stage** environment. When all the testing is done, the live traffic is moved to **Stage** which becomes the **Prod** environment, and current **Prod** environment becomes the **Stage**. There is added benefit of a fast rollback by just changing route if new issue is found with live traffic.

# Checker App

Just run it in Docker:
```yml
>docker run -d -p 5050:5050 bbenetskyy/checker:stable
```


# Version App

## Creating WebApi
For testing Blue/Green Deployment we will create simple API which will return just his current version.

Let's start from creating .Net Core WebApi project with name `VersionApi`
```powershell
> dotnet new webapi -n VersionApi
The template "ASP.NET Core Web API" was created successfully.
This template contains technologies from parties other than Microsoft, see https://aka.ms/template-3pn for details.

Processing post-creation actions...
Running 'dotnet restore' on VersionApi\VersionApi.csproj...
  Restoring packages for .\VersionApi\VersionApi.csproj...
  Restore completed in 66.95 ms for .\VersionApi\VersionApi.csproj.
  Generating MSBuild file .\VersionApi\obj\VersionApi.csproj.nuget.g.props.
  Generating MSBuild file .\VersionApi\obj\VersionApi.csproj.nuget.g.targets.
  Restore completed in 1.96 sec for .\VersionApi\VersionApi.csproj.

Restore succeeded.
```
Remove default `ValuesController` and add our simple `VersionController`:
```powershell
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace VersionApi.Controllers
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
> dotnet run --project .\VersionApi\
Hosting environment: Production
Content root path: .\VersionApi
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
ENTRYPOINT ["dotnet", "VersionApi.dll"]
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

## Deploy Docker Image

If you are using K8s with minikube, you should configure it to [see local images](https://stackoverflow.com/questions/42564058/how-to-use-local-docker-images-with-minikube).

If you are using K8s with Docker Edge, it should be already discovered.

But we will push it into our account at Docker Hub to be more close to production reality:
```powershell
> docker login
Authenticating with existing credentials...
Login Succeeded
> docker image list
REPOSITORY                                 TAG                      IMAGE ID            CREATED             SIZE
bbenetskyy/version-api                     v0.5                     c165de68b824        4 hours ago         299MB
> #docker tag my_img $docker_name/my_img
> docker push bbenetskyy/version-api
The push refers to repository [docker.io/bbenetskyy/version-api]
549c267c90dc: Pushed
cf35c5700b01: Pushed
b267b4a2dc6c: Mounted from microsoft/dotnet
afd643d90d06: Mounted from microsoft/dotnet
e21eadb9b098: Mounted from microsoft/dotnet
8b15606a9e3e: Mounted from microsoft/dotnet
v0.5: digest: sha256:b1265a7dffff612128e77b1b2513c31858347e8da5a98de4cec9778e50b2e6ab size: 1582
```

## Kubernetes Support

Let's write our definition of Deployment. In our example I will use [K8s v1.10 API](https://v1-10.docs.kubernetes.io/docs/reference/generated/kubernetes-api/v1.10/).

If you don't sure your version of K8s you could check it at `kubectl`:
```powershell
> kubectl version
Client Version: version.Info{Major:"1", Minor:"10", GitVersion:"v1.10.3", GitCommit:"2bba0127d85d5a46ab4b778548be28623b32d0b0", GitTreeState:"clean", BuildDate:"2018-05-21T09:17:39Z", GoVersion:"go1.9.3", Compiler:"gc", Platform:"windows/amd64"}
Server Version: version.Info{Major:"1", Minor:"10", GitVersion:"v1.10.3", GitCommit:"2bba0127d85d5a46ab4b778548be28623b32d0b0", GitTreeState:"clean", BuildDate:"2018-05-21T09:05:37Z", GoVersion:"go1.9.3", Compiler:"gc", Platform:"linux/amd64"}
```

According to API we need to use `apiVersion: apps/v1beta1` for Deployment. 

We also want to have 10 containers(`replicas: 10`) with our image(`image: bbenetskyy/version-api:v0.5`). 

According to standard's policy we also specify resources memory(`memory: "64M"`) and cpy(`cpu: "250m"`) limit's.

Open at minion port 80 for container - `containerPort: 80`.

Define labels - `app: version-api` and containers names - `name: version-api-pod`

Here is complete YAML template:
```yml
apiVersion: apps/v1beta1
kind: Deployment
metadata:
  name: version-api
spec:
  replicas: 10
  template:
    metadata:
      labels:
        app: version-api
    spec:
      containers:
      - name: version-api-pod
        image: bbenetskyy/version-api:v0.5
        ports:
        - containerPort: 80
        #resources:
        #  requests:
        #    memory: "1024M"
        #    cpu: "2"
        #  limits:
        #    memory: "2048M"
        #    cpu: "4"
```
> For me locally limits resources does not working and each time returns `0/1 nodes are available: 1 Insufficient cpu.` for all nodes except 1 or 2 of 10.

For open it outside we need to specify a Service.

According to API we need to use `apiVersion: v1` for Service. 

Open outside port 80(`port: 80`) and 80 to container(`targetPort: 80`). For nodePort we can't assign 80 because provided port is not in the valid range. The range of valid ports is 30000-32767 - `nodePort: 30500`

Select our nodes by labels defined for Deployment `app: version-api`

Set what type of Service we want - `type: LoadBalancer`


Here is complete YAML template:
```yml
apiVersion: v1
kind: Service
metadata:
  name: version-api-service
spec:
  ports:
    - name: http
      port: 80
      targetPort: 80
      nodePort: 30500
  selector:
    app: version-api
  type: LoadBalancer
```

Now we could apply it:
```powershell
> kubectl apply -f .\deploy_v0.5.yml
deployment.apps "version-api" created
service "version-api-service" created
> kubectl apply -f .\deploy_v1.0.yml --record
deployment.apps "version-api" configured
> kubectl rollout  history deploy version-api
deployments "version-api"
REVISION  CHANGE-CAUSE
1         <none>
2         kubectl.exe apply --filename=.\deploy_v1.0.yml --record=true
> kubectl rollout  undo deploy version-api --to-revision=1
deployment.apps "version-api"
PS C:\Users\bbenetskyi\Desktop\k8s-green-blue-deployment\K8s\ramped> k rollout  history deploy version-api
deployments "version-api"
REVISION  CHANGE-CAUSE
2         kubectl.exe apply --filename=.\deploy_v1.0.yml --record=true
3         <none>
> kubectl scale --replicas=1 deploy version-api --record
deployment.extensions "version-api" scaled
```

## [K8s Deployment Strategies](https://github.com/ContainerSolutions/k8s-deployment-strategies)

## Azure Blue/Green Deployment

Create all in Azure Portal and after it login in PowerShell:
```powershell
> az aks get-credentials --resource-group vm-ps-test --name k8s-test-name
> kubectl config current-context
```
Install dashboard:
```powershell
> az aks install-cli
> az aks browse --resource-group vm-ps-test --name k8s-test-name
```
When you create a static public IP address for use with AKS, the IP address resource must be created in the node resource group. Get the resource group name with the `az aks show` command and add the `--query nodeResourceGroup` query parameter:
```powershell
> az aks show --resource-group vm-ps-test --name k8s-test-name  --query nodeResourceGroup  -o tsv
MC_vm-ps-test_k8s-test-name_westeurope
```
Now create a static public IP address with the `az network public ip create` command. Specify the node resource group name obtained in the previous command:
```powershell
> az network public-ip create `
>>     --resource-group MC_vm-ps-test_k8s-test-name_westeurope `
>>     --name k8s-test-name `
>>     --allocation-method static
{
  "publicIp": {
    "dnsSettings": null,
    "etag": "******",
    "id": "******",
    "idleTimeoutInMinutes": 4,
    "ipAddress": "13.94.171.227",
    "ipConfiguration": null,
    "ipTags": [],
    "location": "westeurope",
    "name": "k8s-test-name",
    "provisioningState": "Succeeded",
    "publicIpAddressVersion": "IPv4",
    "publicIpAllocationMethod": "Static",
    "resourceGroup": "MC_vm-ps-test_k8s-test-name_westeurope",
    "resourceGuid": "******",
    "sku": {
      "name": "Basic",
      "tier": "Regional"
    },
    "tags": null,
    "type": "Microsoft.Network/publicIPAddresses",
    "zones": null
  }
}
```
You can later get the public IP address using the `az network public-ip list` command. Specify the name of the node resource group, and then query for the ipAddress as shown in the following example:
```powershell
> az network public-ip list --resource-group MC_vm-ps-test_k8s-test-name_westeurope --query [0].ipAddress --output tsv
13.94.171.227
```
Add it into your svc and change type to load balancer:
```yml
apiVersion: v1
kind: Service
metadata:
  name: api-svc
spec:
  loadBalancerIP: 13.94.171.227
  type: LoadBalancer
  ports:
    - name: http
      port: 80
      targetPort: 80
      nodePort: 30500
  selector:
    app: v0.5
```
Apply changes and you will see your application at http://13.94.171.227:80/