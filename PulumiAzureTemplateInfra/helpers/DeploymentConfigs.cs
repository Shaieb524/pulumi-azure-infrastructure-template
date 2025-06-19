using System.Collections.Generic;
using System.Text.Json;
using Pulumi;

namespace PulumiAzureTemplateInfra.helpers
{
    public record DeploymentConfigs
    {
        public string Location { get; init; }
        public string Prefix { get; init; }
        public string Environment { get; init; }
        public string DeploymentKind { get; init; }
        public string ConfigNameSeparator { get; init; }

        // Docker settings
        public Dictionary<string, string> DockerSettings { get; init; }
        public string ApiLinuxFxVersion { get; init; }
        public string FnLinuxFxVersion { get; init; }

        // Resource names
        public Dictionary<string, string> ResourcesNames { get; init; }

        // Database settings
        public Dictionary<string, string> Database { get; init; }

        // Tags
        public Dictionary<string, string> CommonTags { get; init; }

        // Plan SKU
        public Dictionary<string, object> PlanSku { get; init; }

        // App-specific configurations 
        public Dictionary<string, object> ApiAppSettings { get; init; }
        public Dictionary<string, object> FnAppSettings { get; init; }

        // Secure secret access
        public SecretAccess Secrets { get; init; }

        public DeploymentConfigs(Config config)
        {
            Location = config.Require("Location");
            Prefix = config.Require("Prefix");
            Environment = config.Require("Environment");
            DeploymentKind = config.Require("DeploymentKind");

            CommonTags = config.RequireObject<Dictionary<string, string>>("Tags");
            PlanSku = config.RequireObject<Dictionary<string, object>>("PlanSku");
            ResourcesNames = config.RequireObject<Dictionary<string, string>>("ResourcesNames");
            DockerSettings = config.RequireObject<Dictionary<string, string>>("DockerSettings");
            Database = config.RequireObject<Dictionary<string, string>>("Database");

            var apiSettingsRaw = config.RequireObject<JsonElement>("ApiAppSettings");
            ApiAppSettings = ConfigParser.ConvertJsonElementToDictionary(apiSettingsRaw);

            var fnSettingsRaw = config.RequireObject<JsonElement>("FnAppSettings");
            FnAppSettings = ConfigParser.ConvertJsonElementToDictionary(fnSettingsRaw);

            // Secret access
            Secrets = new SecretAccess(config);

            // Docker FX versions
            ApiLinuxFxVersion = $"DOCKER|{DockerSettings["DockerRegistryUrl"]}/{DockerSettings["DockerApiImageName"]}:{DockerSettings["DockerApiImageTag"]}";
            FnLinuxFxVersion = $"DOCKER|{DockerSettings["DockerRegistryUrl"]}/{DockerSettings["DockerFnImageName"]}:{DockerSettings["DockerFnImageTag"]}";

            ConfigNameSeparator = DeploymentKind.ToLower() == "linux" ? "__" : ":";
        }
    }
}