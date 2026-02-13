namespace GistAutomation.Utils;

public static class AzureServiceNameNormalizer
{
    private static readonly Dictionary<string, string> AliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["app service"] = "Azure App Service",
        ["azure app service"] = "Azure App Service",
        ["web apps"] = "Azure App Service",
        ["azure web apps"] = "Azure App Service",

        ["functions"] = "Azure Functions",
        ["azure functions"] = "Azure Functions",

        ["storage"] = "Azure Storage",
        ["azure storage"] = "Azure Storage",
        ["storage accounts"] = "Azure Storage",
        ["blob storage"] = "Azure Blob Storage",
        ["azure blob storage"] = "Azure Blob Storage",
        ["table storage"] = "Azure Table Storage",
        ["queue storage"] = "Azure Queue Storage",
        ["file storage"] = "Azure Files",
        ["azure files"] = "Azure Files",

        ["key vault"] = "Azure Key Vault",
        ["azure key vault"] = "Azure Key Vault",
        ["keyvault"] = "Azure Key Vault",

        ["sql database"] = "Azure SQL Database",
        ["azure sql database"] = "Azure SQL Database",
        ["azure sql"] = "Azure SQL Database",
        ["cosmos db"] = "Azure Cosmos DB",
        ["azure cosmos db"] = "Azure Cosmos DB",
        ["cosmosdb"] = "Azure Cosmos DB",

        ["virtual network"] = "Azure Virtual Network",
        ["azure virtual network"] = "Azure Virtual Network",
        ["vnet"] = "Azure Virtual Network",
        ["load balancer"] = "Azure Load Balancer",
        ["azure load balancer"] = "Azure Load Balancer",
        ["application gateway"] = "Azure Application Gateway",
        ["azure application gateway"] = "Azure Application Gateway",
        ["azure firewall"] = "Azure Firewall",
        ["firewall"] = "Azure Firewall",

        ["virtual machines"] = "Azure Virtual Machines",
        ["azure virtual machines"] = "Azure Virtual Machines",
        ["vm"] = "Azure Virtual Machines",
        ["vms"] = "Azure Virtual Machines",

        ["azure active directory"] = "Microsoft Entra ID",
        ["azure ad"] = "Microsoft Entra ID",
        ["aad"] = "Microsoft Entra ID",
        ["entra id"] = "Microsoft Entra ID",
        ["microsoft entra id"] = "Microsoft Entra ID",

        ["cognitive search"] = "Azure AI Search",
        ["azure cognitive search"] = "Azure AI Search",
        ["azure search"] = "Azure AI Search",
        ["azure ai search"] = "Azure AI Search",
        ["ai search"] = "Azure AI Search",

        ["monitor"] = "Azure Monitor",
        ["azure monitor"] = "Azure Monitor",
        ["log analytics"] = "Azure Log Analytics",
        ["azure log analytics"] = "Azure Log Analytics",
        ["application insights"] = "Azure Application Insights",
        ["azure application insights"] = "Azure Application Insights",

        ["container instances"] = "Azure Container Instances",
        ["azure container instances"] = "Azure Container Instances",
        ["aci"] = "Azure Container Instances",
        ["kubernetes service"] = "Azure Kubernetes Service",
        ["azure kubernetes service"] = "Azure Kubernetes Service",
        ["aks"] = "Azure Kubernetes Service",

        ["service bus"] = "Azure Service Bus",
        ["azure service bus"] = "Azure Service Bus",
        ["event hubs"] = "Azure Event Hubs",
        ["azure event hubs"] = "Azure Event Hubs",
        ["event grid"] = "Azure Event Grid",
        ["azure event grid"] = "Azure Event Grid",

        ["azure devops"] = "Azure DevOps",
        ["devops"] = "Azure DevOps",

        ["azure backup"] = "Azure Backup",
        ["backup"] = "Azure Backup",
        ["azure site recovery"] = "Azure Site Recovery",
        ["site recovery"] = "Azure Site Recovery",
        ["azure dns"] = "Azure DNS",
        ["dns"] = "Azure DNS",
        ["azure cdn"] = "Azure CDN",
        ["cdn"] = "Azure CDN",
    };

    public static string Normalize(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return rawName;

        var cleaned = string.Join(' ', rawName.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));

        if (AliasMap.TryGetValue(cleaned, out var canonical))
            return canonical;

        if (!cleaned.StartsWith("Azure ", StringComparison.OrdinalIgnoreCase) &&
            !cleaned.StartsWith("Microsoft ", StringComparison.OrdinalIgnoreCase))
        {
            return $"Azure {cleaned}";
        }

        return cleaned;
    }
}
