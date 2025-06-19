using System;
using System.Collections.Generic;
using System.Linq;
using Pulumi;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Pulumi.AzureNative.Storage;
using PulumiAzureTemplateInfra.helpers;

namespace PulumiAzureTemplateInfra
{
    public partial class ContainerizedStack
    {
        #region Function App
        private static WebApp CreateFunctionApp(DeploymentConfigs deploymentConfigs, ResourceGroup resourceGroup)
        {
            // Storage Account for Functions
            var functionStorageAccount = new StorageAccount(deploymentConfigs.ResourcesNames["FnStorageAccountName"], new StorageAccountArgs
            {
                AccountName = deploymentConfigs.ResourcesNames["FnStorageAccountName"],
                ResourceGroupName = resourceGroup.Name,
                Location = resourceGroup.Location,
                Tags = deploymentConfigs.CommonTags,
                Kind = Pulumi.AzureNative.Storage.Kind.StorageV2,
                Sku = new Pulumi.AzureNative.Storage.Inputs.SkuArgs
                {
                    Name = SkuName.Standard_LRS
                }
            });

            // Function App Service Plan
            var appServicePlan = new AppServicePlan(deploymentConfigs.ResourcesNames["FnAppPlanName"], new AppServicePlanArgs
            {
                Name = $"{deploymentConfigs.ResourcesNames["FnAppPlanName"]}",
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
                    Capacity = Convert.ToInt32(deploymentConfigs.PlanSku["Capacity"].ToString())
                }
            });

            // Application Insights for Functions
            var appInsights = new Component(deploymentConfigs.ResourcesNames["FnAppInsightsName"], new ComponentArgs
            {
                ResourceName = $"{deploymentConfigs.ResourcesNames["FnAppInsightsName"]}",
                ResourceGroupName = resourceGroup.Name,
                Location = resourceGroup.Location,
                Tags = deploymentConfigs.CommonTags,
                ApplicationType = ApplicationType.Web,
                Kind = "web"
            });

            // Function App
            var functionApp = new WebApp(deploymentConfigs.ResourcesNames["FnAppName"], new WebAppArgs
            {
                Name = $"{deploymentConfigs.ResourcesNames["FnAppName"]}",
                Location = resourceGroup.Location,
                ResourceGroupName = resourceGroup.Name,
                Kind = "functionapp,linux",
                ServerFarmId = appServicePlan.Id,
                SiteConfig = new SiteConfigArgs
                {
                    AlwaysOn = true,
                    LinuxFxVersion = deploymentConfigs.FnLinuxFxVersion,
                    AppSettings = GetFunctionAppSettings(deploymentConfigs, appInsights, resourceGroup.Name, functionStorageAccount.Name),
                    HealthCheckPath = deploymentConfigs.FnAppSettings["HealthCheck"].ToString() ?? "/api/health",
                },
                Tags = deploymentConfigs.CommonTags
            }, new CustomResourceOptions { 
                DependsOn = { functionStorageAccount, appServicePlan }
            });

            return functionApp;
        }

        private static NameValuePairArgs[] GetFunctionAppSettings(DeploymentConfigs deploymentConfigs, Component appInsights, Output<string> resourceGroupName, Output<string> storageAccountName)
        {
            var storageConnectionString = CreateStorageConnectionString(resourceGroupName, storageAccountName);
            var secrets = deploymentConfigs.Secrets;

            var settings = new List<NameValuePairArgs>
            {
                // Core Function settings
                new() { Name = "AzureWebJobsStorage", Value = storageConnectionString },
                new() { Name = "FUNCTIONS_WORKER_RUNTIME", Value = "dotnet-isolated" },
                new() { Name = "FUNCTIONS_EXTENSION_VERSION", Value = "~4" },
                new() { Name = "WEBSITE_RUN_FROM_PACKAGE", Value = "0" }, // essesential for containers
                new() { Name = "WEBSITES_ENABLE_APP_SERVICE_STORAGE", Value = "false" }, // essesential for containers
                new() { Name = "AzureWebJobsSecretStorageType", Value = "files" },
                
                // Application Insights
                new() { Name = "APPLICATIONINSIGHTS_CONNECTION_STRING", Value = appInsights.ConnectionString },
                
                // Docker settings
                new() { Name = "DOCKER_REGISTRY_SERVER_URL", Value = $"https://{deploymentConfigs.DockerSettings["DockerRegistryUrl"]}" },
                new() { Name = "DOCKER_REGISTRY_SERVER_USERNAME", Value = deploymentConfigs.DockerSettings["DockerRegistryUserName"] },
                new() { Name = "DOCKER_REGISTRY_SERVER_PASSWORD", Value = secrets.DockerRegistryPassword }
            };

            // Add function-specific settings
            AddFunctionSettings(settings, deploymentConfigs);

            return settings.ToArray();
        }

        private static void AddFunctionSettings(List<NameValuePairArgs> settings, DeploymentConfigs deploymentConfigs)
        {
            var fnSettings = deploymentConfigs.FnAppSettings;
            var secrets = deploymentConfigs.Secrets;

            if (fnSettings.TryGetValue("Values", out var valuesObj) && valuesObj is Dictionary<string, object> valuesDict)
            {
                foreach (var kvp in valuesDict)
                {
                    // Skip core settings we already handled
                    if (IsCoreFunctionSetting(kvp.Key))
                        continue;

                    settings.Add(new() { Name = kvp.Key, Value = kvp.Value?.ToString() ?? "" });
                }
            }

            // Add connection settings
            if (fnSettings.TryGetValue("Connections", out var connObj) && connObj is Dictionary<string, object> connDict)
            {
                foreach (var kvp in connDict)
                {
                    var value = kvp.Key switch
                    {
                        "EventGridEndpoint" => kvp.Value?.ToString() ?? "",
                        "EventGridKey" => secrets.EventGridKey.Apply(k => k).ToString() ?? "",
                        "SignalRConnectionString" => secrets.SignalRConnectionString.Apply(k => k).ToString() ?? "",
                        _ => kvp.Value?.ToString() ?? ""
                    };

                    settings.Add(new() { Name = kvp.Key, Value = value });
                }
            }
        }

        private static bool IsCoreFunctionSetting(string key)
        {
            var coreSettings = new[] {
                "FUNCTIONS_WORKER_RUNTIME",
                "FUNCTIONS_EXTENSION_VERSION",
                "WEBSITE_RUN_FROM_PACKAGE",
                "AzureWebJobsSecretStorageType"
            };
            return coreSettings.Contains(key);
        }

        private static Output<string> CreateStorageConnectionString(Output<string> resourceGroupName, Output<string> accountName)
        {
            var storageAccountKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
            {
                ResourceGroupName = resourceGroupName,
                AccountName = accountName
            });

            return storageAccountKeys.Apply(keys =>
            {
                var primaryStorageKey = keys.Keys[0].Value;
                return Output.Format($"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={primaryStorageKey};EndpointSuffix=core.windows.net");
            });
        }
        #endregion
    }
}
