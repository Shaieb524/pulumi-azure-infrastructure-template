using System;
using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.EventGrid;
using PulumiAzureTemplateInfra.helpers;

namespace PulumiAzureTemplateInfra
{
    public partial class ContainerizedStack
    {
        #region Event Grid Topic
        private static Topic CreateEventGridTopic(DeploymentConfigs deploymentConfigs, ResourceGroup resourceGroup)
        {
            var eventGridTopic = new Topic(deploymentConfigs.ResourcesNames["EventGridTopicName"], new TopicArgs
            {
                TopicName = deploymentConfigs.ResourcesNames["EventGridTopicName"],
                Location = resourceGroup.Location,
                ResourceGroupName = resourceGroup.Name,
                Tags = deploymentConfigs.CommonTags,
            });

            return eventGridTopic;
        }
        #endregion
    }
}
