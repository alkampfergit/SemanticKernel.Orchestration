using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Assistants;
using SemanticKernel.Orchestration.Orchestrators;
using System.ComponentModel;
using System.Text;

namespace SemanticKernel.Orchestration.SampleAgents.Common;

public class SummaryAssistant : BaseAssistant
{
    private readonly KernelStore _kernelStore;

    public const string SummaryAssistantAgentName = "SummaryAssistant";

    public SummaryAssistant(
        KernelStore kernelStore) : base(SummaryAssistantAgentName)
    {
        RegisterFunctionDelegate(
            "Summarize",
            KernelFunctionFactory.CreateFromMethod(Summarize),
            async (args) => await Summarize(
                args["propertyName"].ToString()!,
                args["context"]?.ToString()),
            isFinal: false);
        _kernelStore = kernelStore;
    }

    [Description("Summarize a text contained in an assistant property using an optional context!")]
    private async Task<AssistantResponse> Summarize(
        [Description("The name of the property to get")]
        string propertyName,
        [Description("The context for the summarization")]
        string? context)
    {
        //need to grab property from the orchestrator
        var property = _orchestrator.GetProperty(propertyName);
        if (property == null)
        {
            throw new Exception($"Property {propertyName} not found in current orchestration");
        }

        var kernel = _kernelStore.GetKernel("gpt4omini");
        StringBuilder prompt = new StringBuilder();
        if (string.IsNullOrEmpty(context))
        {
            prompt.AppendLine("You will summarize the following text:");
        }
        else
        {
            prompt.AppendLine("You will summarize the following text using a context as a guideline");
            prompt.AppendLine($"Contexext: {context}");
        }

        prompt.AppendLine($"\nText:\n{property}");

        var result = await kernel.InvokePromptAsync(prompt.ToString());
        var stringResult = result.ToString();
        SetGlobalProperty("summarization", stringResult);
        return "Summary saved in summarization property";
    }
}
