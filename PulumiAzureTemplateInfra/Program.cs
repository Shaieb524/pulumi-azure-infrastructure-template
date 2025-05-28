using System.Threading.Tasks;
using PulumiAzureTemplateInfra;
using Pulumi;

class Program
{
    static Task<int> Main() => Deployment.RunAsync<ContainerizedStack>();
}