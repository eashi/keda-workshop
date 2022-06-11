# Welcome to the KEDA Workshop

In this workshop we will learn what [KEDA](https://github/kedacore/keda) is, how it works, what are the built-in scalers, and how to build an External scaler specific to our needs.

## Pre-requisites
- [Docker Desktop](https://docs.docker.com/get-docker/)
- [Minikube](https://kubernetes.io/docs/tasks/tools/install-minikube/) or Desktop Desktop with Kubernetes enabled 
- [Kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/) 
- [Helm](https://helm.sh/docs/intro/install/)
- [.NET Core 6.0](https://dotnet.microsoft.com/download/dotnet-core) 

## Workshop Flow 1 (built in scalers)

### Install KEDA
There are several ways to install KEDA, the simplest one is to use the Helm charts.


1. Add Helm repo

`helm repo add kedacore https://kedacore.github.io/charts`

2. Update Helm repo

`helm repo update`

3. Install KEDA Helm chart

```
kubectl create namespace keda
helm install keda kedacore/keda --namespace keda
```
For other options check KEDA's [deployment documentation](https://keda.sh/docs/1.4/deploy/).

### Follow the RabbitMQ Sample

Clone https://github.com/eashi/sample-go-rabbitmq.git, and **check out the branch `simple-secret`**. *(this is a fork of the official RabbitMQ sample, the original can be found [here](https://github.com/kedacore/sample-go-rabbitmq))*

Follow the instructions of the sample **until you reach "Deploying a RabbitMQ consumer"** so we can discuss the `deploy/deploy-consumer.yaml` file.

## Workshop Flow 2 (External Scalers)

### Create a target deployment
This is the deployment that we target for scaling, it can be the deployment that consumes messages from a queue for example. 

In our case it is just a simple [web app sample](https://hub.docker.com/r/microsoft/aci-helloworld) that is provided by microsoft. We are not going to worry about exposing it with a Service; the purpose of the workshop is just to show how this deployment will scale up and down based on our External scaler.

To deploy the target app:
`kubectl apply -f my-scaler/yaml/target-deployment.yaml`

### Create The External Scaler

External scalers are containers that implement provide gRPC endpoints. So let's create a gRPC .NET Core app from the 

1. `dotnet new grpc -n my-scaler`
2. Add the file `externalscaler.proto` from [https://github.com/kedacore/keda/blob/master/pkg/scalers/externalscaler/externalscaler.proto](https://github.com/kedacore/keda/blob/master/pkg/scalers/externalscaler/externalscaler.proto) to the folder `my-scaler/my-scaler/Protos`
3. Include the file we just created in the gRPC code generation by adding the following line to the .csproj file: 
```
<Protobuf Include="Protos\externalscaler.proto" GrpcServices="Server" />
```
4. Run `dotnet build` to generate the base gRPC code
5. Create a file `ExternalScalerService.cs` under Services folder, we will build it gradually together. Otherwise, you can copy the file from this repo if you want to jump to its final state. 
6. Add the following line in the `Startup.cs` file in the `UseEndpoints` section
```
endpoints.MapGrpcService<ExternalScalerService>();
```
7. Add the following line in the `Startup.cs` file in the `ConfigureServices` method
```
services.AddHttpClient();
```
8. Create a `Dockerfile` file, and a `.dockerignore` file (**it's important not to forget this one**, you can copy the content from the repo)
9. Build the image by running:
```
docker build . -t my-scaler-image
``` 
Feel free to choose the image name you like, however remember to use it all the way down after this point.

### Create a Deployment and a Service in Kubernetes for our new scaler
Let's create a Deployment and a Service to run our scaler and service requests to. From the root of this repo copy the file `my-scaler/yaml/my-scaler-deployment.yaml`
, and then run:
```
kubectl apply -f my-scaler-deployment.yaml
```

### Create a mock server
In this step we are going to create a fake http endpoint. Our scaler will query this endpoint, and it will return an integer that we will use as the fake criteria on which we are going to scale our deployment on.

In reality this might be a length of queue of a technology that does not have a built-in support in KEDA, or number of logged in users...etc.

1. In this repo, open the file `mock-server/mockserver-config/static/initializerJson.json` and create a new endpoint that returns an integer in a string format. Let's call it `fake`.

2. Create the namespace "mockserver":
```
kubectl create namespace mockserver
```

3. Navigate to the folder `mock-server` and then run the following command to create a configmpa from which the mockserver will read the configuration. 
```
helm upgrade --install --namespace mockserver mockserver-config mockserver-config
```

4. Create a deployment to run the mockserver itself. run the following:
```
helm upgrade --install --namespace mockserver --set app.mountConfigMap=true --set app.mountedConfigMapName=mockserver-config --set app.propertiesFileNamem=mockserver.properties --set app.initializationJsonFileName=initializerJson.json mockserver mockserver
```
5. If you want to change the configuration to experiment the scaling up and down, run the following command to restart the mockserve and force it to take the new config values:
```
kubectl rollout restart deploy/mockserver -n mockserver
```

### Create the ScaledObject
ScaledObject is the kubernetes resource (specific to KEDA) that will tell KEDA to scale our target deployment based on the configuration within. You copy the content of the file from `my-scaler/yaml/scaled-config.yaml`. And then run:

```
kubectl apply -f scaled-config.yaml 
```

If everything is setup right, and fake endpoint returns the right value, then watch your target deployment scaling out to many pods.