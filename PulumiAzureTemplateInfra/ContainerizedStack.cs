using System;
using System.Collections.Generic;
using System.Linq;
using Pulumi;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.SignalRService;
using Pulumi.AzureNative.EventGrid;

namespace PulumiAzureTemplateInfra
{
    public class ContainerizedStack : Pulumi.Stack
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

            // Outputs
            this.ResourceGroupName = resourceGroup.Name;
            this.ApiUrl = apiAppService.DefaultHostName.Apply(hostname => $"https://{hostname}");
        }

        [Output] public Output<string> ResourceGroupName { get; set; }
        [Output] public Output<string> ApiUrl { get; set; }


        #region API App Service
        private static WebApp CreateApiAppService(DeploymentConfigs deploymentConfigs, ResourceGroup resourceGroup)
        {
            // App Service Plan
            var appServicePlan = new AppServicePlan($"{deploymentConfigs.ResourcesNames["ApiAppServicePlanName"]}", new AppServicePlanArgs
            {
                Name = $"{deploymentConfigs.ResourcesNames["ApiAppServicePlanName"]}",
                Location = resourceGroup.Location,
                Kind = "linux",
                ResourceGroupName = resourceGroup.Name,
                Tags = deploymentConfigs.CommonTags,
                Reserved = true,
                Sku = new SkuDescriptionArgs
                {
                    Name = deploymentConfigs.PlanSku["Name"].ToString()!,
                    Tier = deploymentConfigs.PlanSku["Tier"].ToString()!,
                    Size = deploymentConfigs.PlanSku["Size"].ToString()!,
                    Family = deploymentConfigs.PlanSku["Family"].ToString()!,
                    Capacity = int.Parse(deploymentConfigs.PlanSku["Capacity"].ToString()!)
                }
            });

            // Application Insights
            var appInsights = new Component($"{deploymentConfigs.ResourcesNames["ApiAppInsightsName"]}", new ComponentArgs
            {
                ResourceName = $"{deploymentConfigs.ResourcesNames["ApiAppInsightsName"]}",
                ResourceGroupName = resourceGroup.Name,
                Location = resourceGroup.Location,
                Tags = deploymentConfigs.CommonTags,
                ApplicationType = ApplicationType.Web,
                Kind = "web"
            });

            // App Service
            var appService = new WebApp($"{deploymentConfigs.ResourcesNames["ApiAppServiceName"]}", new WebAppArgs
            {
                Name = $"{deploymentConfigs.ResourcesNames["ApiAppServiceName"]}",
                Location = resourceGroup.Location,
                ResourceGroupName = resourceGroup.Name,
                Kind = "app,linux,container",
                ServerFarmId = appServicePlan.Id,
                SiteConfig = new SiteConfigArgs
                {
                    AlwaysOn = true,
                    LinuxFxVersion = deploymentConfigs.ApiLinuxFxVersion,
                    AppSettings = GetApiAppSettings(deploymentConfigs, appInsights),
                    ConnectionStrings = GetApiConnectionStrings(deploymentConfigs),
                    HealthCheckPath = deploymentConfigs.ApiAppSettings["HealthCheck"].ToString() ?? "/health",
                },
                Tags = deploymentConfigs.CommonTags
            });

            return appService;
        }

        private static NameValuePairArgs[] GetApiAppSettings(DeploymentConfigs deploymentConfigs, Component appInsights)
        {
            var settings = new List<NameValuePairArgs>
            {
                // Core settings
                new() { Name = "WEBSITE_RUN_FROM_PACKAGE", Value = "1" },
                new() { Name = "APPLICATIONINSIGHTS_CONNECTION_STRING", Value = appInsights.ConnectionString },
                
                // Docker settings
                new() { Name = "DOCKER_REGISTRY_SERVER_URL", Value = $"https://{deploymentConfigs.DockerSettings["DockerRegistryUrl"]}" },
                new() { Name = "DOCKER_REGISTRY_SERVER_USERNAME", Value = deploymentConfigs.DockerSettings["DockerRegistryUserName"] },
                new() { Name = "DOCKER_REGISTRY_SERVER_PASSWORD", Value = deploymentConfigs.Secrets.DockerRegistryPassword }
            };

            // Add simplified app settings
            AddApiSettings(settings, deploymentConfigs);

            return settings.ToArray();
        }

        private static void AddApiSettings(List<NameValuePairArgs> settings, DeploymentConfigs deploymentConfigs)
        {
            var apiSettings = deploymentConfigs.ApiAppSettings;
            var secrets = deploymentConfigs.Secrets;

            // Basic settings
            if (apiSettings.TryGetValue("DisableHttpsRedirection", out var disableHttps))
                settings.Add(new() { Name = "DisableHttpsRedirection", Value = disableHttps.ToString()! });

            // External API settings
            if (apiSettings.TryGetValue("ExternalApi", out var extApiObj) && extApiObj is Dictionary<string, object> extApiDict)
            {
                foreach (var kvp in extApiDict)
                {
                    settings.Add(new() { Name = $"ExternalApi__{kvp.Key}", Value = kvp.Value?.ToString() ?? "" });
                }
                settings.Add(new() { Name = "ExternalApi__ApiKey", Value = secrets.ExternalApiKey });
            }

            // Feature flags
            if (apiSettings.TryGetValue("Features", out var featuresObj) && featuresObj is Dictionary<string, object> featuresDict)
            {
                foreach (var kvp in featuresDict)
                {
                    settings.Add(new() { Name = $"Features__{kvp.Key}", Value = kvp.Value?.ToString() ?? "" });
                }
            }

            // Business settings
            if (apiSettings.TryGetValue("Business", out var businessObj) && businessObj is Dictionary<string, object> businessDict)
            {
                foreach (var kvp in businessDict)
                {
                    settings.Add(new() { Name = $"Business__{kvp.Key}", Value = kvp.Value?.ToString() ?? "" });
                }
            }
        }

        private static ConnStringInfoArgs[] GetApiConnectionStrings(DeploymentConfigs deploymentConfigs)
        {
            var connectionStrings = new List<ConnStringInfoArgs>();
            var secrets = deploymentConfigs.Secrets;

            // Database connection
            connectionStrings.Add(new ConnStringInfoArgs
            {
                Name = "DefaultConnection",
                ConnectionString = secrets.BuildSqlConnectionString("MyAppDatabase"),
                Type = ConnectionStringType.SQLAzure
            });

            // Blob storage connection
            connectionStrings.Add(new ConnStringInfoArgs
            {
                Name = "BlobStorage",
                ConnectionString = secrets.BuildBlobConnectionString(deploymentConfigs.Database["BlobStorageAccount"], secrets.BlobStorageKey),
                Type = ConnectionStringType.Custom
            });

            return connectionStrings.ToArray();
        }
        #endregion
    }
}