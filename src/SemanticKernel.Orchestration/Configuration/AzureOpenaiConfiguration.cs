using System;

namespace SemanticKernel.Orchestration.Configuration;

public record AzureOpenaiConfiguration(string ApiBase, string ApiKey, string DeploymentName)
{
    public bool IsValid() => !string.IsNullOrWhiteSpace(ApiBase) 
        && !string.IsNullOrWhiteSpace(ApiKey) 
        && !string.IsNullOrWhiteSpace(DeploymentName);
}
