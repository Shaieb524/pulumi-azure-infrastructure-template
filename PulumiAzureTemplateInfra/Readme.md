# Pulumi Azure Infrastructure Template

A **production-ready Pulumi template** for deploying **containerized applications** to Azure with all essential services.

## 🏗️ **What This Template Deploys**

### **Core Infrastructure:**
- ✅ **Resource Group** - Organized resource container
- ✅ **App Service** - **Containerized web application** (Linux + Docker)
- ✅ **Function App** - **Containerized serverless** background processing
- ✅ **SignalR Service** - Real-time communication
- ✅ **Event Grid Topic** - Event-driven messaging
- ✅ **Application Insights** - Monitoring and telemetry
- ✅ **Storage Accounts** - Function runtime and blob storage

### **🐳 Containerized Architecture:**
This template is specifically designed for **containerized microservices** where:
- **API applications** run in Docker containers on Azure App Service
- **Function apps** use custom container images for background processing
- **Consistent deployment** patterns across all services
- **Docker registry integration** with secure credential management

### **Security Features:**
- 🔐 **Encrypted secrets** management via Pulumi
- 🔒 **Secure connection strings** built dynamically
- 🏷️ **Consistent tagging** and naming conventions
- 🛡️ **Environment separation** (dev/staging/prod)

## 🎯 **Architectural Philosophy & Design Decisions**

### **📋 Standardized YAML Structure for Microservices**
This template uses a **custom-designed YAML configuration approach** to ensure **consistency across all microservices** in your organization:

```yaml
ServiceName:ApiAppSettings:
  ExternalApi:
    BaseUrl: https://api.example.com
    Timeout: 30
  Features:
    EnableCaching: true
    MaxRetries: 3
```

**Why this approach?**
- 🏢 **Team Standardization** - Every microservice follows the same configuration pattern
- 🔄 **Easy Onboarding** - New team members instantly understand any service's config
- 📋 **Consistent Structure** - Same sections (ApiAppSettings, FnAppSettings, Secrets) across all services
- 🛠️ **Maintainable** - One person learns the pattern, everyone benefits

### **🔧 Dynamic JSON/YAML Parser (ConfigParser.cs)**
The `ConfigParser` class handles **nested configuration structures** dynamically:

```csharp
// Automatically converts complex YAML structures to C# dictionaries
var apiSettings = ConfigParser.ConvertJsonElementToDictionary(yamlSection);

// Access nested values without hardcoding structure
if (apiSettings.TryGetValue("ExternalApi", out var extApiObj) && 
    extApiObj is Dictionary<string, object> extApiDict)
{
    // Dynamic access to any nested configuration
}
```

**Benefits of this approach:**
- ✅ **YAML-First** - Add new config sections without touching C# code
- ✅ **No Code Regeneration** - Update YAML, redeploy - no compilation needed for config changes
- ✅ **Flexible Structure** - Support any nesting level or configuration complexity
- ✅ **Type-Safe** - Runtime type checking with graceful fallbacks

### **🌟 Alternative Approaches (and why we chose this one)**
There are **many ways** to handle configuration in .NET/Pulumi:

| Approach | Pros | Cons | Our Choice |
|----------|------|------|------------|
| **Strongly-typed C# classes** | Compile-time safety | Requires code changes for new config | ❌ Too rigid |
| **Direct Pulumi Config calls** | Simple | Scattered throughout code | ❌ Not maintainable |
| **JSON Schema validation** | Structured | Complex tooling | ❌ Over-engineered |
| **Environment variables** | Simple | Hard to manage at scale | ❌ Not scalable |
| **Our Dynamic YAML + Parser** | Flexible + Consistent | Slightly more complex setup | ✅ **Perfect balance** |

### **🏗️ Why This Matters for Microservices**
When you have **10+ microservices**, you need:
- **Consistent patterns** - Same config structure across all services
- **Team efficiency** - Anyone can understand any service's configuration
- **Rapid deployment** - Change config without rebuilding containers
- **Standardized secrets** - Same security patterns everywhere

This template provides exactly that foundation.

## 🚀 **Quick Start**

### **1. Prerequisites**
```bash
# Install Pulumi CLI
# Install .NET 6.0 SDK
# Install Azure CLI and login: az login
```

### **2. Clone and Customize**
```bash
# Clone this template
git clone <your-template-repo>
cd PulumiAzureTemplateInfra

# Customize for your project:
# - Update Pulumi.dev.yaml with your settings
# - Change namespace in C# files (find/replace "PulumiAzureTemplateInfra")
# - Update resource names and Docker images
```

### **3. Set Secrets**
```bash
# Update the script with your actual secret values
./add-secrets.ps1
```

### **4. Deploy**
```bash
# Restore packages
dotnet restore

# Deploy infrastructure
pulumi up
```

## 📁 **Project Structure**

```
PulumiAzureTemplateInfra/
├── Pulumi.yaml                    # Main project config
├── Pulumi.dev.yaml                # Environment-specific settings
├── SecretAccess.cs                # Secure secret management
├── DeploymentConfigs.cs           # Configuration parser
├── ContainerizedStack.cs          # Main infrastructure logic
├── ConfigParser.cs                # JSON utility functions
├── Program.cs                     # Entry point
├── PulumiAzureTemplateInfra.csproj # Project file
├── add-secrets.ps1                # Secret setup script
└── README.md                      # This file
```

## ⚙️ **Customization Guide**

### **For Your Project:**

1. **Update Names:**
   - Change `PulumiAzureTemplateInfra` to `YourApp` in all files
   - Update namespace: `PulumiAzureTemplateInfra` → `YourAppInfrastructure`
   - Modify resource names in `Pulumi.dev.yaml`

2. **Configure Docker Images:**
   ```yaml
   DockerSettings:
     DockerRegistryUrl: yourregistry.azurecr.io
     DockerApiImageName: your-api-image
     DockerFnImageName: your-functions-image
   ```

3. **Containerized Applications:**
   - This template assumes your applications are **containerized**
   - **API Service**: Your web API packaged as a Docker container
   - **Function App**: Your Azure Functions in a custom container image
   - **Docker Registry**: Secure access to your container registry (ACR recommended)
   - **Linux App Service Plans** - Optimized for containers

4. **Add Your App Settings:**
   ```yaml
   ApiAppSettings:
     YourFeature:
       Setting1: value1
       Setting2: value2
   ```

5. **Update Secrets:**
   - Modify `add-secrets.ps1` with your actual secret keys
   - Add/remove secrets in `SecretAccess.cs` as needed

## 🎯 **Architecture Patterns**

This template demonstrates **enterprise-grade patterns**:

### **Configuration Management:**
- **YAML-based** environment-specific configuration
- **Type-safe** C# configuration classes
- **Separation** of secrets from regular config

### **Secret Management:**
- **Encrypted at rest** using Pulumi's secret encryption
- **Runtime injection** without exposure in code/config
- **Connection string builders** for dynamic assembly

### **Resource Organization:**
- **Consistent naming** with environment prefixes
- **Proper tagging** for cost management and organization
- **Logical grouping** in resource groups

### **Scalability:**
- **Basic tier** for development (easily upgradable)
- **Container-ready** for modern deployment practices
- **Monitoring included** with Application Insights

## 🔧 **Advanced Usage**

### **Multiple Environments:**
```bash
# Create additional environment configs
cp Pulumi.dev.yaml Pulumi.staging.yaml
cp Pulumi.dev.yaml Pulumi.prod.yaml

# Deploy to different environments
pulumi up --stack staging
pulumi up --stack prod
```

### **Add New Services:**
Extend `ContainerizedStack.cs` to add more Azure services:
- Azure SQL Database
- Redis Cache
- API Management
- Key Vault
- And more...

### **Custom App Settings:**
Add your application-specific configuration in `Pulumi.dev.yaml`:
```yaml
ApiAppSettings:
  YourCustomSection:
    DatabaseTimeout: 30
    CacheExpiry: 3600
    FeatureFlags:
      EnableNewFeature: true
```

## 🌟 **Benefits of This Template**

- ⚡ **Quick Setup** - Deploy in minutes, not hours
- 🛡️ **Security First** - Proper secret management built-in
- 📈 **Scalable** - Enterprise patterns that grow with your needs
- 🔄 **Repeatable** - Consistent deployments across environments
- 🎯 **Best Practices** - Following Azure and DevOps standards
- 🏢 **Team Standardization** - Same patterns across all microservices
- 🐳 **Container-Ready** - Built for modern containerized applications
- 🔧 **Configuration Flexibility** - Update YAML without code changes
- 📋 **Microservices-Friendly** - Designed for multi-service architectures

## 💡 **Next Steps**

1. **Customize** the template for your specific needs
2. **Create your containerized applications** and push to container registry
3. **Set up CI/CD** pipelines using this infrastructure template
4. **Monitor** your applications with the included Application Insights
5. **Scale to multiple microservices** by replicating this pattern
6. **Standardize your team** on this configuration approach

### **🚀 For Microservices Teams:**
- **Clone this template** for each microservice
- **Maintain the same YAML structure** across all services
- **Share configuration patterns** between teams
- **Onboard new developers** faster with consistent patterns
- **Scale infrastructure** confidently with proven patterns

---

**Happy coding!** 🚀 This template gives you a solid foundation for modern containerized microservices on Azure.