﻿using FluentAssertions;
using Jarvis.Common.Shared.Utils.SqlUtils;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2;
using SemanticKernel.Orchestration.Configuration;
using SemanticKernel.Orchestration.Orchestrators;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static SemanticKernel.Orchestration.Assistants.SampleAssistantDemo2.SqlServerSchemaAssistant;

namespace SemanticKernel.Orchestration.Tests.HelperTests;

public class SqlHelperTests
{
    static SqlHelperTests()
    {
        System.Data.Common.DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);
        var config = new SqlServerConfiguration();
        DataAccess.SetConnectionString(config.ConnectionString, "Microsoft.Data.SqlClient", NullLogger.Instance);
    }
    private SqlServerSchemaAssistant _sut;

    public SqlHelperTests(KernelStore kernelStore)
    {
        _sut = new SqlServerSchemaAssistant(kernelStore);
        _sut.InitializeWithSharedState(new SqlServerSharedState());
    }

    [Fact]
    public async Task Can_grab_database_list()
    {
        var dbList = await _sut.GetDatabaseList();
        dbList.State.Should().BeOfType<List<string>>();
    }

    [Fact]
    public async Task Can_query_table_list()
    {
        var dbList = await _sut.RetrieveTableSchema("northwind");
        dbList.State.Should().BeOfType<DatabaseSchema>();

        var dbSchema = dbList.State as DatabaseSchema;
        dbSchema.Tables.Count.Should().Be(14);
    }
}
