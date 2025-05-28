# Pulumi Azure Infrastructure Template

A production-ready Pulumi template for deploying containerized applications to Azure with all essential services.

## What This Deploys

- Resource Group - Organized resource container
- App Service - Containerized web application (Linux + Docker)
- Function App - Containerized serverless background processing
- SignalR Service - Real-time communication
- Event Grid Topic - Event-driven messaging
- Application Insights - Monitoring and telemetry
- Storage Accounts - Function runtime and blob storage

![Deployed Resources](docs/deployed-resources.png)

*Example of deployed Azure resources after running `pulumi up`*

## Quick Start

### 1. Prerequisites
```bash
# Install Pulumi CLI, .NET 6.0 SDK, Azure CLI
az login
```

### 2. Clone and Customize
```bash
git clone <your-template-repo>
cd PulumiAzureTemplateInfra

# Update Pulumi.dev.yaml with your settings:
# - Resource names, Docker images, environment details
```

### 3. Set Secrets
```bash
./add-secrets.ps1
# Update script with your actual secret values first
```

### 4. Deploy
```bash
dotnet restore
pulumi up
```

## Why This Template?

### Built for Containerized Microservices
- Assumes your applications run in Docker containers
- Consistent deployment patterns across all services
- Secure Docker registry integration

### Standardized Configuration
Every microservice uses the same YAML structure:
```yaml
ServiceName:ApiAppSettings:
  ExternalApi:
    BaseUrl: https://api.example.com
    Timeout: 30
  Features:
    EnableCaching: true
```

### No Code Changes for Config Updates
The ConfigParser handles nested YAML dynamically - add new config sections without touching C# code.

### Team Consistency
- Same patterns across all microservices
- New developers understand any service instantly
- Faster code reviews and debugging

## Customization

### For Your Project:

1. **Update Names**
   - Change `PulumiAzureTemplateInfra` to `YourApp` in all files
   - Update resource names in `Pulumi.dev.yaml`

2. **Configure Docker Images**
   ```yaml
   DockerSettings:
     DockerRegistryUrl: yourregistry.azurecr.io
     DockerApiImageName: your-api-image
     DockerFnImageName: your-functions-image
   ```

3. **Add Your App Settings**
   ```yaml
   ApiAppSettings:
     YourFeature:
       Setting1: value1
       Setting2: value2
   ```

4. **Update Secrets**
   - Modify `add-secrets.ps1` with your actual values
   - Add/remove secrets in `SecretAccess.cs` as needed

## Project Structure

```
PulumiAzureTemplateInfra/
├── Pulumi.yaml                    # Main project config
├── Pulumi.dev.yaml                # Environment settings
├── SecretAccess.cs                # Secure secret management
├── DeploymentConfigs.cs           # Configuration parser
├── ContainerizedStack.cs          # Infrastructure logic
├── ConfigParser.cs                # Dynamic YAML parser
├── Program.cs                     # Entry point
├── add-secrets.ps1                # Secret setup script
└── README.md                      # This file
```

## Security Features

- Encrypted secrets management via Pulumi
- Secure connection strings built dynamically  
- Consistent tagging for cost management
- Environment separation (dev/staging/prod)

## Next Steps

1. **Customize** the template for your needs
2. **Create your containerized applications** and push to registry
3. **Set up CI/CD** pipelines using this infrastructure
4. **Scale to multiple microservices** by replicating this pattern

---

Perfect for teams building containerized microservices on Azure.