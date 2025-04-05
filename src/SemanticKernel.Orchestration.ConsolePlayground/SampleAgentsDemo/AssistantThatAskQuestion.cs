using System;
using System.Text;
using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Assistants;
using SemanticKernel.Orchestration.Orchestrators;

namespace SemanticKernel.Orchestration.ConsolePlayground.SampleAgentsDemo;

public class AssistantThatAskQuestion: BaseAssistant
{
    public AssistantThatAskQuestion(KernelStore kernelStore) : base("JokeAssistant")
    {
        RegisterFunctionDelegate(
            "TellMeAJoke",
            KernelFunctionFactory.CreateFromMethod(TellMeAJoke),
            async (args) => await TellMeAJoke());
        _kernelStore = kernelStore;
    }

    private readonly KernelStore _kernelStore;

    public async Task<AssistantResponse> TellMeAJoke()
    {
        var kernel = _kernelStore.GetKernel("gpt4omini");

        StringBuilder prompt = new StringBuilder();
        prompt.AppendLine($@"you are a famous comedian, the user aked you for a specific joke,
        you need to generate a question for the user so you can have a better joke for him. If you think that you have
        enout information you can just say 'I have a joke for you' and then tell the joke.");

        if (_jokeInstructions.Length > 0)
        {
            prompt.AppendLine("Current set of question and answers:");
            prompt.AppendLine(_jokeInstructions.ToString());
        }
        else
        {
            //this is the original question
            var context = KernelStore.GetActiveConversationContext();
            _jokeInstructions.AppendLine("\nOriginal Joke Request: " + context.CurrentUserQuestion);
            prompt.AppendLine("Original Joke Request: " + context.CurrentUserQuestion);
        }

        var result = await kernel.InvokePromptAsync(prompt.ToString());

        var response = result.ToString();
        if (response.Contains("I have a joke for you"))
        {
            return AssistantResponse.FinalResponse(response);
        }

        _jokeInstructions.AppendLine("Question:" + response);
        return AssistantResponse.AskUser(response);
    }

    public override Task<AssistantResponse?> AnswerQuestion(string answer, CancellationToken cancellationToken = default)
    {
        _jokeInstructions.AppendLine("Answer:" + answer);
        return TellMeAJoke()!;
    }

    private StringBuilder _jokeInstructions = new StringBuilder();
}
