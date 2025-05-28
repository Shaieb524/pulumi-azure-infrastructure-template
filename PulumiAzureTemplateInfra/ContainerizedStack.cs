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
using Pulumi.AzureNative.SignalRService.Inputs;


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

            // 3. Function App
            var functionApp = CreateFunctionApp(deploymentConfigs, resourceGroup);

            // 4. SignalR Service
            var signalRService = CreateSignalRService(deploymentConfigs, resourceGroup);


            // Outputs
            this.ResourceGroupName = resourceGroup.Name;
            this.ApiUrl = apiAppService.DefaultHostName.Apply(hostname => $"https://{hostname}");
            this.FunctionUrl = functionApp.DefaultHostName.Apply(hostname => $"https://{hostname}");
            this.SignalREndpoint = signalRService.HostName.Apply(hostname => $"https://{hostname}");

        }

        [Output] public Output<string> ResourceGroupName { get; set; }
        [Output] public Output<string> ApiUrl { get; set; }
        [Output] public Output<string> FunctionUrl { get; set; }
        [Output] public Output<string> SignalREndpoint { get; set; }


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
                    AlwaysOn = false,
                    LinuxFxVersion = deploymentConfigs.FnLinuxFxVersion,
                    AppSettings = GetFunctionAppSettings(deploymentConfigs, appInsights, resourceGroup.Name, functionStorageAccount.Name),
                    HealthCheckPath = deploymentConfigs.FnAppSettings["HealthCheck"].ToString() ?? "/api/health",
                },
                Tags = deploymentConfigs.CommonTags
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
                new() { Name = "WEBSITE_RUN_FROM_PACKAGE", Value = "1" },
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