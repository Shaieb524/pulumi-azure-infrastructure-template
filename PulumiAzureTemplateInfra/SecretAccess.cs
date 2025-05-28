using Pulumi;
using System;
using System.Collections.Generic;

namespace PulumiAzureTemplateInfra
{
    // PulumiAzureTemplateInfra secret access template
    public class SecretAccess
    {
        private readonly Config _config;

        public SecretAccess(Config config)
        {
            _config = config;
        }

        // Core secrets - essential for any app
        public Output<string> DockerRegistryPassword => _config.RequireSecret("DockerRegistryPassword");
        public Output<string> SqlPassword => _config.RequireSecret("SqlPassword");
        public Output<string> BlobStorageKey => _config.RequireSecret("BlobStorageKey");
        public Output<string> AppInsightsConnectionString => _config.RequireSecret("AppInsightsConnectionString");

        // External service secrets
        public Output<string> ExternalApiKey => _config.RequireSecret("ExternalApiKey");
        public Output<string> EventGridKey => _config.RequireSecret("EventGridKey");
        public Output<string> SignalRConnectionString => _config.RequireSecret("SignalRConnectionString");

        // Function storage
        public Output<string> FunctionStorageKey => _config.RequireSecret("FunctionStorageKey");

        // Helper method to build SQL connection string
        public Output<string> BuildSqlConnectionString(string database)
        {
            var databaseObj = _config.RequireObject<Dictionary<string, string>>("Database");
            var server = databaseObj.ContainsKey("SqlServer") ? databaseObj["SqlServer"] : throw new ArgumentException("SQL Server not configured");
            var userId = databaseObj.ContainsKey("SqlUserId") ? databaseObj["SqlUserId"] : throw new ArgumentException("SQL User ID not configured");

            return SqlPassword.Apply(pwd =>
                $"server=tcp:{server};User ID={userId};Password={pwd};Encrypt=true;database={database}");
        }

        // Helper method to build blob connection string
        public Output<string> BuildBlobConnectionString(string accountName, Output<string> accountKey)
        {
            return accountKey.Apply(key =>
                $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={key};EndpointSuffix=core.windows.net");
        }
    }
}