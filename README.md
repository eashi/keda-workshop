# Welcome to the KEDA Workshop

In this workshop we will learn what [KEDA](https://github/kedacore/keda) is, how it works, what are the built-in scalers, and how to build an External scaler specific to our needs.

## Pre-requisites
- [Docker Desktop](https://docs.docker.com/get-docker/)
- [Minikube](https://kubernetes.io/docs/tasks/tools/install-minikube/) or Desktop Desktop with Kubernetes enabled 
- [Kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/) 
- [Helm](https://helm.sh/docs/intro/install/)
- [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core) 

## Workshop Flow

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

### Create a target deployment
This is the deployment that we target for scaling, it can be the deployment that consumes messages from a queue for example. 

In our case it is just a simple [web app sample](https://hub.docker.com/r/microsoft/aci-helloworld) that is provided by microsoft. We are not going to worry about exposing it with a Service; the purpose of the workshop is just to show how this deployment will scale up and down based on our External scaler.

To deploy the target app:
`kubectl apply -f my-scaler/yaml/target-deployment.yaml`

### Create The External Scaler

External scalers are containers that implement provide gRPC endpoints. So let's create a gRPC .NET Core app from the 

1. `dotnet new grpc -n my-scaler`
2. Add the file `externalscaler.proto` from [this location](https://github.com/kedacore/keda/blob/master/pkg/scalers/externalscaler/externalscaler.proto) to the folder `my-scaler/my-scaler/Protos`
3. Include the file we just created in the gRPC code generation by adding the following line to the .csproj file: 

`<Protobuf Include="Protos\externalscaler.proto" GrpcServices="Server" />`

4. Run `dotnet build` to generate the base gRPC code
5. Create a file `ExternalScalerService.cs` under Services folder. You can copy the file from this repo if you want to jump to its final state.


