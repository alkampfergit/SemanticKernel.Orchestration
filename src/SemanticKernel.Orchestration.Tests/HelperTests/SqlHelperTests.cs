using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticKernel.Orchestration.Helpers;
using SemanticKernel.Orchestration.Orchestrators;
using SemanticKernel.Orchestration.SampleAgents.SqlServer;
using SemanticKernel.Orchestration.SampleAgents.SqlServer.SqlUtils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static SemanticKernel.Orchestration.SampleAgents.SqlServer.SqlServerSchemaAssistant;

namespace SemanticKernel.Orchestration.Tests.HelperTests;

public class SqlHelperTests
{
    static SqlHelperTests()
    {
        System.Data.Common.DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);
        
        var config = new SqlServerConfiguration();
        
        var connectionString = Environment.GetEnvironmentVariable("SQL_SERVER_TEST_CONNECTION") 
            ?? config.ConnectionString;
            
        DataAccess.SetConnectionString(connectionString, "Microsoft.Data.SqlClient", NullLogger.Instance);
    }
    private SqlServerSchemaAssistant _sut;
    private NullLoggerFactory _loggerFactory;
    private ServiceCollection _serviceCollection;
    private ServiceProvider _serviceProvider;

    public SqlHelperTests()
    {
        _loggerFactory = NullLoggerFactory.Instance;
        SemanticOrchestratorLoggerFactory.Init(_loggerFactory);
        _serviceCollection = new ServiceCollection();
        _serviceProvider = _serviceCollection.BuildServiceProvider();
        KernelStore kernelStore = new KernelStore(_serviceProvider);
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
        var dbList = await _sut.RetrieveDatabaseSchema("northwind");
        dbList.State.Should().BeOfType<DatabaseSchema>();

        var dbSchema = dbList.State as DatabaseSchema;
        dbSchema.Should().NotBeNull();
        dbSchema.Tables.Count.Should().BeGreaterThan(10);
    }
}
