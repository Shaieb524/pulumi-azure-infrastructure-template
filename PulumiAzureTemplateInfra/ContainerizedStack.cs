using System;
using Pulumi;
using Pulumi.AzureNative.Resources;

namespace PulumiAzureTemplateInfra
{
    public partial class ContainerizedStack : Pulumi.Stack
    {
        public ContainerizedStack()
        {
            var config = new Config("PulumiAzureTemplateInfra");
            var deploymentConfigs = new DeploymentConfigs(config);

            // 1. Resource Group
            var resourceGroup = new ResourceGroup("resourcegroup", new ResourceGroupArgs
            {
                ResourceGroupName = $"{deploymentConfigs.Prefix}-{deploymentConfigs.ResourcesNames["ResourceGroupName"]}",
                Location = deploymentConfigs.Location,
                Tags = deploymentConfigs.CommonTags
            });

            // 2. App Service (API)
            var apiAppService = CreateApiAppService(deploymentConfigs, resourceGroup);

            // 3. Function App
            var functionApp = CreateFunctionApp(deploymentConfigs, resourceGroup);

            // 4. SignalR Service
            var signalRService = CreateSignalRService(deploymentConfigs, resourceGroup);

            // 5. Event Grid Topic
            var eventGridTopic = CreateEventGridTopic(deploymentConfigs, resourceGroup);


            // Outputs
            this.ResourceGroupName = resourceGroup.Name;
            this.ApiUrl = apiAppService.DefaultHostName.Apply(hostname => $"https://{hostname}");
            this.FunctionUrl = functionApp.DefaultHostName.Apply(hostname => $"https://{hostname}");
            this.SignalREndpoint = signalRService.HostName.Apply(hostname => $"https://{hostname}");
            this.EventGridEndpoint = eventGridTopic.Endpoint;

        }

        [Output] public Output<string> ResourceGroupName { get; set; }
        [Output] public Output<string> ApiUrl { get; set; }
        [Output] public Output<string> FunctionUrl { get; set; }
        [Output] public Output<string> SignalREndpoint { get; set; }
        [Output] public Output<string> EventGridEndpoint { get; set; }
    }
}