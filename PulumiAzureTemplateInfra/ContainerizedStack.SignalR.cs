using System;
using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.SignalRService;
using Pulumi.AzureNative.SignalRService.Inputs;

namespace PulumiAzureTemplateInfra
{
    public partial class ContainerizedStack
    {
        #region SignalR Service
        private static SignalR CreateSignalRService(DeploymentConfigs deploymentConfigs, ResourceGroup resourceGroup)
        {
            var signalRService = new SignalR(deploymentConfigs.ResourcesNames["SignalRName"], new SignalRArgs
            {
                ResourceName = deploymentConfigs.ResourcesNames["SignalRName"],
                Location = resourceGroup.Location,
                ResourceGroupName = resourceGroup.Name,
                Tags = deploymentConfigs.CommonTags,
                Sku = new ResourceSkuArgs
                {
                    Name = "Free_F1",
                    Tier = "Free",
                    Capacity = 1
                },
            });

            return signalRService;
        }
        #endregion
    }
}
