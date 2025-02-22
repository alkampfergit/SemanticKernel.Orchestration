using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants;

/// <summary>
/// An assistant is capable of interacting with the kernel
/// and orchestrating stuff.
/// </summary>
public abstract class BaseAssistant : IConversationOrchestrator
{
    private readonly string _name;
    private readonly Dictionary<string, string> _properties = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, FunctionInfo> _functions = new(StringComparer.OrdinalIgnoreCase);

    protected IConversationOrchestrator? _orchestrator;

    /// <summary>
    /// Contains information about a function that can be executed by the current
    /// orchestrator.
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="KernelFunction"></param>
    /// <param name="Function"></param>
    /// <param name="IsFinal"></param>
    /// <param name="CanBeExecuted"></param>
    public record FunctionInfo(
        string Name,
        KernelFunction KernelFunction,
        Func<IDictionary<string, object>,
        Task<AssistantResponse>> Function,
        bool IsFinal,
        Func<CancellationToken, Task<bool>> CanBeExecuted);

    /// <summary>
    /// Create an assistant you must give the name
    /// </summary>
    /// <param name="name"></param>
    public BaseAssistant(string name)
    {
        _name = name;
    }

    public string Name => _name;

    public virtual string InjectedPrompt => string.Empty;

    /// <summary>
    /// Set the orchestrator this agent refers to.
    /// </summary>
    /// <param name="orchestrator"></param>
    public virtual void SetOrchestrator(IConversationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    private static Task<bool> _canExecute(CancellationToken _) => Task.FromResult(true);

    protected void RegisterFunctionDelegate(
        string functionName,
        KernelFunction kernelFunction,
        Func<IDictionary<string, object>, Task<AssistantResponse>> function,
        bool isFinal = false,
        Func<CancellationToken, Task<bool>>? canExecute = null)
    {
        _functions[functionName] = new FunctionInfo(functionName, kernelFunction, function, isFinal, canExecute ?? _canExecute);
    }

    public virtual void AddResultToPrompt(ChatHistory chatHistory, AssistantResponse agentOperationResult)
    {
        chatHistory.AddAssistantMessage(agentOperationResult.Result);
    }

    public virtual string GetFact(AssistantResponse agentOperationResult)
    {
        return agentOperationResult.Result;
    }

    public async Task<IReadOnlyCollection<FunctionInfo>> GetFunctionsAsync(CancellationToken cancellationToken = default)
    {
        var functions = new List<FunctionInfo>();
        foreach (var function in _functions.Values)
        {
            if (await function.CanBeExecuted(cancellationToken))
            {
                functions.Add(function);
            }
        }
        return functions;
    }

    public async Task<AssistantResponse> ExecuteFunctionAsync(string function, IDictionary<string, object> arguments)
    {
        if (!_functions.ContainsKey(function))
        {
            throw new ArgumentException($"Function {function} not found");
        }

        Console.WriteLine($"Executing function {function} with parameters: {DumpArguments(arguments)}");
        var functionInfo = _functions[function];
        return await functionInfo.Function(arguments);
    }

    private object DumpArguments(IDictionary<string, object> arguments)
    {
        return string.Join(", ", arguments.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    public virtual string GetAssistantProperty(string propertyName)
    {
        if (_properties.TryGetValue(propertyName, out var value))
        {
            return value;
        }

        throw new ArgumentException($"Property {propertyName} not found");
    }

    protected void SetLocalProperty(string propertyName, string value)
    {
        _properties[propertyName] = value;
    }

    protected void SetGlobalProperty(string propertyName, string value)
    {
        _orchestrator.AddProperty(propertyName, value);
    }

    public void AddProperty(string propertyName, string value)
    {
        SetLocalProperty(propertyName, value);
    }

    public string? GetProperty([Description("Property name")] string propertyName)
    {
        return _properties.TryGetValue(propertyName, out var value) ? value : null;
    }
}
