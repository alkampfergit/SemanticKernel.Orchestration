using System;
using System.Collections.Generic;

namespace SemanticKernel.Orchestration.Assistants;

/// <summary>
/// When TerminateCycle is true, the agent want to terminate the cycle, let the user prosecute
/// with questions, and finally we need to print the state to the user or if the state is null
/// print Result
/// </summary>
/// <param name="Result"></param>
/// <param name="State"></param>
/// <param name="TerminateCycle">If true the orchestrator will terminate the cycle and return the result to the user. The 
/// result can be the state of the Assistant response if it is a simple string or the Result property</param>
/// <param name="Question">If different from null it means that the cycle must be terminated, the question is 
/// the value that should be returned to the user. In the next step the text of the user must be sent directly to the  
/// agent</param>
public record AssistantResponse(string Result, object? State = null, bool TerminateCycle = false, bool IsUserQuestion = false)
{
    public static implicit operator AssistantResponse(string result) => new AssistantResponse(result);

    public static AssistantResponse AskUser(string question) => new AssistantResponse(question, IsUserQuestion: true);

    public static AssistantResponse FinalResponse(string response) => new AssistantResponse(response, TerminateCycle: true);
}
