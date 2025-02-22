﻿using Jarvis.Common.Shared.Utils.SqlUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernel.Orchestration.Configuration;
using SemanticKernel.Orchestration.Helpers;
using SemanticKernel.Orchestration.Orchestrators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2;

/// <summary>
/// This is a complex assistant, that uses other assistants to perform some
/// work.
/// </summary>
public class SqlServerAssistant : BaseAssistant
{
    private readonly KernelStore _kernelStore;
    private readonly SqlServerSchemaAssistant _sqlServerSchemaAssistant;
    private readonly SqlServerQueryExecutor _sqlServerQueryExecutor;
    private readonly SqlServerSharedState _sharedState;
    private const string DefaultModelName = "gpt4omini";

    private Dictionary<string, BaseAssistant> _subAssistants = new(StringComparer.OrdinalIgnoreCase);

    public SqlServerAssistant(
       IUserQuestionManager userQuestionManager,
       KernelStore kernelStore,
       SqlServerConfiguration sqlServerConfiguration,
       [FromKeyedServices("sql")] SqlServerSchemaAssistant sqlServerSchemaAssistant,
       [FromKeyedServices("sql")] SqlServerQueryExecutor sqlServerQueryExecutor) : base("SqlServerAssistant")
    {
        _sharedState = new SqlServerSharedState();
        _kernelStore = kernelStore;
        _sqlServerSchemaAssistant = sqlServerSchemaAssistant;
        _sqlServerQueryExecutor = sqlServerQueryExecutor;
        _subAssistants["schema"] = sqlServerSchemaAssistant;
        _subAssistants["query"] = sqlServerQueryExecutor;

        DataAccess.SetConnectionString(sqlServerConfiguration.ConnectionString, "Microsoft.Data.SqlClient", NullLogger.Instance);

        // Initialize sub-assistants with shared state
        _sqlServerSchemaAssistant.InitializeWithSharedState(_sharedState);
        _sqlServerQueryExecutor.InitializeWithSharedState(_sharedState);

        RegisterFunctionDelegate(
            "ExcuteDatabaseOperation",
            KernelFunctionFactory.CreateFromMethod(ExcuteDatabaseOperation),
            async (args) => await ExcuteDatabaseOperation(args["operationToExecute"].ToString()!));
    }

    internal override void SetOrchestrator(IConversationOrchestrator orchestrator)
    {
        base.SetOrchestrator(orchestrator);
        _sqlServerQueryExecutor.SetOrchestrator(orchestrator);
        _sqlServerSchemaAssistant.SetOrchestrator(orchestrator);
    }

    public override string InjectedPrompt => @"
If the question regards databases and you do not have information in the FACTS, you can call ExcuteDatabaseOperation";

    [Description("Execute various operation in database, executes query in natural language, get schema etc, everything is expressed in natural language")]
    public async Task<AssistantResponse> ExcuteDatabaseOperation(
        [Description("the operation to execute expressed in natural language")]
        string operationToExecute)
    {
        //here we orchestrate the work of the assistants
        var kernel = _kernelStore.GetKernel(DefaultModelName);

        while (true)
        {
            //now we need to enter a cycle where we ask the kernel to solve a sql task
            //ok I need to get all the functions for all the assistants
            List<KernelFunction> functions = new();
            List<string> finalFunctions = new();

            //I need to use the functino of the schema assistant in the prompt
            Dictionary<string, BaseAssistant> assistantMap = new();
            foreach (var subAssistant in _subAssistants.Values)
            {
                foreach (var function in subAssistant.GetFunctions())
                {
                    functions.Add(function.KernelFunction);
                    assistantMap[function.Name] = subAssistant;
                    if (function.IsFinal)
                    {
                        finalFunctions.Add(function.Name);
                    }
                }
            }

            //var settings = new PromptExecutionSettings
            //{
            //    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(functions, autoInvoke: false)
            //};

            var settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(functions, autoInvoke: false),
                Temperature = 0,
            };

            //ChatMessageContent result = await PerformCallWithChatModel(question, kernel, settings, cancellationToken);
            ChatMessageContent result = await PerformCallWithSimplePromptModel(operationToExecute, kernel, settings, CancellationToken.None);

            var response = result.Items.OfType<FunctionCallContent>().SingleOrDefault();
            if (response == null)
            {
                return result.ToString();
            }

            var assistant = assistantMap[response.FunctionName];
            var assistantFunctionCallResult = await assistant.ExecuteFunctionAsync(response.FunctionName, response.Arguments);

            if (finalFunctions.Contains(response.FunctionName) || assistantFunctionCallResult.TerminateCycle)
            {
                return assistantFunctionCallResult;
            }
        }
    }

    private async Task<ChatMessageContent> PerformCallWithSimplePromptModel(
        string question,
        Kernel kernel,
        PromptExecutionSettings settings,
        CancellationToken cancellationToken)
    {
        StringBuilder prompt = new();
        prompt.AppendLine(
            @"You are a sql server assistant, capable of performing query and various operation on the database.
You will be asked to answer to a question. You can use the FACTS that follows to answer the question.
If Facts does not contains data to answer you can call functions to get more FACTS.

FACTS:");

        prompt.AppendLine(_sharedState.ToPromptFact());
        prompt.AppendLine("\nQuestion: " + question);

        var functionResult = await kernel.InvokePromptAsync(prompt.ToString(), new(settings), cancellationToken: cancellationToken);
        return functionResult.GetValue<ChatMessageContent>()!;
    }
}
