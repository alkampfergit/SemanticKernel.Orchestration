﻿using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Assistants;
using SemanticKernel.Orchestration.Orchestrators;
using SemanticKernel.Orchestration.SampleAgents.SqlServer.SqlUtils;
using System.ComponentModel;
using System.Text;

namespace SemanticKernel.Orchestration.SampleAgents.SqlServer;

public class SqlServerSchemaAssistant : BaseAssistant, IConversationOrchestrator
{
    private SqlServerSharedState _sharedState;
    private readonly KernelStore _kernelStore;

    public SqlServerSchemaAssistant(KernelStore kernelStore) : base("SqlServerSchemaAssistant")
    {
        RegisterFunctionDelegate(
             "GetDatabaseList",
             KernelFunctionFactory.CreateFromMethod(GetDatabaseList),
             async (_) => await GetDatabaseList(),
             canExecute: (_) => Task.FromResult(_sharedState == null || _sharedState.SchemaState.DataBaseList?.Any() != true));

        RegisterFunctionDelegate(
             "RetrieveDatabaseSchema",
             KernelFunctionFactory.CreateFromMethod(RetrieveDatabaseSchema),
             async (args) => await RetrieveDatabaseSchema(
                 args["databaseName"].ToString()!),
             canExecute: (_) => Task.FromResult(_sharedState == null || _sharedState.SchemaRetrievedForAllDataBases));

        RegisterFunctionDelegate(
            "GetTableSchemaRepresentation",
            KernelFunctionFactory.CreateFromMethod(GetTableSchemaRepresentation),
            async (args) => await GetTableSchemaRepresentation(
                args["databaseName"].ToString()!,
                args["userQuestion"].ToString()!),
            isFinal: true);
        _kernelStore = kernelStore;
    }

    public void InitializeWithSharedState(SqlServerSharedState sharedState)
    {
        _sharedState = sharedState;
        _sharedState.SetSchemaAssistant(this);
    }

    [Description("Get the list of the database in the server")]
    public Task<AssistantResponse> GetDatabaseList()
    {
        var databaseList = InnerGetDatabaseList();
        return Task.FromResult(new AssistantResponse("retrieved list of database.", databaseList));
    }

    /// <summary>
    /// This is the inner function that get datbase list, is meant to be caller from 
    /// other assistants.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<string> InnerGetDatabaseList()
    {
        //No need to get the list of db if we already have.
        if (_sharedState.SchemaState.DataBaseList != null)
        {
            return _sharedState.SchemaState.DataBaseList;
        }
        var databaseList = DataAccess
                    .CreateQuery("SELECT name FROM sys.databases")
                    .ExecuteList<string>();

        _sharedState.SchemaState.DataBaseList = databaseList;
        return databaseList;
    }

    [Description("Get schema of tables of a database if you need the schema to answer a user question")]
    public async Task<AssistantResponse> GetTableSchemaRepresentation(
        [Description("Name of the database")] string databaseName,
        [Description("The question of the user regarding the schema")] string userQuestion)
    {
        if (!_sharedState.SchemaState.DatabaseSchema.TryGetValue(databaseName, out var databaseSchema))
        {
            InnerGetDatabaseSchema(databaseName);
        }

        //ok the user wants an answer for the schema, we need to call an llm to answer
        var kernel = _kernelStore.GetKernel("gpt4omini");
        var answer = await kernel.InvokePromptAsync($@"You will answer the user question using the information of database schema that 
are contained in the prompt, you should never use anything else than the included schema to answer the question

question: {userQuestion}

SCHEMA:
{databaseSchema.ToPrompt()}");

        return new AssistantResponse(answer.ToString(), TerminateCycle: true);
    }

    [Description("Query the database for table schema if you didn't already loaded")]
    public async Task<AssistantResponse> RetrieveDatabaseSchema(
        [Description("Name of the database")] string databaseName)
    {
        if (_sharedState.SchemaState.DatabaseSchema.ContainsKey(databaseName))
        {
            //ok that is a typical loop, where the language model does not understand that we already 
            //have the schema of this database.
        }

        DatabaseSchema databaseSchema = InnerGetDatabaseSchema(databaseName);
        return new AssistantResponse("retrieved list of tables.", databaseSchema);
    }

    public DatabaseSchema InnerGetDatabaseSchema(string databaseName)
    {
        //Check if we already has the schema, if not need to retrieve again
        if (_sharedState.SchemaState.DatabaseSchema.TryGetValue(databaseName, out var databaseSchema))
        {
            return databaseSchema;
        }

        var newConnection = ConnectionManager.ChangeDatabase(DataAccess.ConnectionString, databaseName);

        var tableList = DataAccess
            .CreateQueryOn(
            newConnection,
            @"SELECT
                s.name AS SchemaName,
                t.name AS TableName,
                c.name AS ColumnName,
                tp.name AS DataType
            FROM sys.schemas s
            INNER JOIN sys.tables t ON s.schema_id = t.schema_id
            INNER JOIN sys.columns c ON t.object_id = c.object_id
            INNER JOIN sys.types tp ON c.user_type_id = tp.user_type_id
            ORDER BY SchemaName, TableName, ColumnName;")
            .Hydrate(dr => new RawSchemaInfo(
                dr["SchemaName"] as string,
                dr["TableName"] as string,
                dr["ColumnName"] as string,
                dr["DataType"] as string));

        //now I have all the table, for each table I need to get the columns

        var tables = tableList
            .GroupBy(t => new TableNameInfo(t.SchemaName, t.TableName))
            .Select(g => new TableInfo(
                g.Key,
                g.Select(t => new TableColumnInfo(
                    t.ColumnName,
                    t.DataType))
                .ToList()))
            .ToList();

        databaseSchema = new(tables);
        _sharedState.SchemaState.DatabaseSchema[databaseName] = databaseSchema;
        return databaseSchema;
    }

    public record DatabaseSchema(IReadOnlyCollection<TableInfo> Tables)
    {
        internal AssistantResponse ToPrompt()
        {
            StringBuilder stringBuilder = new();
            foreach (var table in Tables)
            {
                stringBuilder.AppendLine($"Table {table.TableNameInfo.SchemaName}.{table.TableNameInfo.TableName}");

                foreach (var column in table.Columns)
                {
                    stringBuilder.AppendLine($"- {column.ColumnName} : {column.DataType}");
                }

                stringBuilder.AppendLine($"End schema of table {table.TableNameInfo.SchemaName}.{table.TableNameInfo.TableName}");
            }

            return stringBuilder.ToString();
        }
    }
}
