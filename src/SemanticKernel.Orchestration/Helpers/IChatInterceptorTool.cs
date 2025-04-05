using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Assistants;
using SemanticKernel.Orchestration.Assistants;

namespace SemanticKernel.Orchestration.Helpers;

/// <summary>
/// Interface for ac omponent that will be called after the chat completion
/// and could inspect the input parameters as well as the respondse content
/// of the LLM
/// </summary>
public interface IChatInterceptorTool
{
    Task OnChatCompletionAsync(
        IReadOnlyList<ChatMessageContent> returnValue,
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        CancellationToken cancellationToken);
}

/// <summary>
/// This interface will be called before calling the real LLM implementation
/// and if it returns a NON null response, that response will be returned to 
/// the caller, this will allow for testing and some advanced scenario
/// </summary>
public interface IChatWrappingTool
{
    /// <summary>
    /// This method will be called before the real chat implementation and if
    /// it returns a NON null response, that response will be returned to the caller
    /// </summary>
    /// <param name="chatHistory"></param>
    /// <param name="executionSettings"></param>
    /// <param name="kernel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<ChatMessageContent>?> OnChatWrappingAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings,
        Kernel? kernel,
        CancellationToken cancellationToken);
}

/// <summary>
/// 
/// </summary>
public class ConversationContext : IDisposable
{
    public IReadOnlyCollection<IChatInterceptorTool> Interceptors => _interceptors;
    private readonly List<IChatInterceptorTool> _interceptors;

    public IReadOnlyCollection<IChatWrappingTool> Wrappers => _wrappers;
    private readonly List<IChatWrappingTool> _wrappers;

    public Dictionary<string, object> Properties { get; } = new();

    /// <summary>
    /// Contains the original user question that started current round
    /// of agent call.
    /// </summary>
    public string? CurrentUserQuestion { get; set; }

    /// <summary>
    /// Every agent/orchestrator can, in any moment, wait for a question that will be
    /// asked to the user. This means that the current question is suspended, a question
    /// is sent to the user as answer, then the next user interation is supposed to be
    /// the answer to the question.
    /// </summary>
    public AgentPendingQuestion? CurrentAgentPendingQuestion { get; set; }

    public ConversationContext(
        IChatInterceptorTool[] interceptors,
        IChatWrappingTool[] wrappers)
    {
        _interceptors = interceptors.ToList();
        _wrappers = wrappers.ToList();
    }

    internal void AddWrapper(IChatWrappingTool callLimiterTool)
    {
        _wrappers.Add(callLimiterTool);
    }

    /// <summary>
    /// GEt an interceptor of a specific type if present, if not
    /// it will return null
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? GetInterceptor<T>() where T : class
    {
          return Interceptors.OfType<T>().FirstOrDefault()
            ?? Wrappers.OfType<T>().FirstOrDefault();
    }

    public void Dispose()
    {
        foreach (var interceptor in Interceptors)
        {
            if (interceptor is IDisposable disposableInterceptor)
            {
                disposableInterceptor.Dispose();
            }
        }

        foreach (var wrapper in Wrappers)
        {
            if (wrapper is IDisposable disposableWrapper)
            {
                disposableWrapper.Dispose();
            }
        }
    }

    internal void AddCurrentUserQuestion(string question)
    {
        CurrentUserQuestion = question;
    }

    /// <summary>
    /// This is called when the agent is waiting for a question
    /// </summary>
    /// <param name="question"></param>
    /// <param name="assistant"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void SetAgentQuestion(string question, BaseAssistant assistant)
    {
        if (CurrentAgentPendingQuestion != null)
        {
            throw new InvalidOperationException("Already waiting for a question");
        }

        CurrentAgentPendingQuestion = new AgentPendingQuestion(question, assistant);
    }

    public async Task<AssistantResponse> AnswerQuestion(string answer, CancellationToken cancellationToken = default)
    {
        if (CurrentAgentPendingQuestion == null)
        {
            throw new InvalidOperationException("Not waiting for a question");
        }

        var result = await CurrentAgentPendingQuestion.Assistant.AnswerQuestion(answer, cancellationToken);
        CurrentAgentPendingQuestion = null;
        return result;
    }

    public record AgentPendingQuestion(string Question, BaseAssistant Assistant);
}
