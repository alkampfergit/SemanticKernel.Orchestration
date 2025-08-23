using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SemanticKernel.Orchestration.Helpers;
using SemanticKernel.Orchestration.Orchestrators;
using SemanticKernel.Orchestration.SampleAgents.Common;
using Xunit;

namespace SemanticKernel.Orchestration.Tests.SampleAgents;

/// <summary>
/// Unit tests for ExcelAssistant functionality that focus on testable logic
/// </summary>
public class ExcelAssistantTests
{
    private readonly Mock<IHumanInTheLoop> _mockHumanInTheLoop;
    private readonly KernelStore _kernelStore;
    private readonly ExcelAssistant _excelAssistant;

    public ExcelAssistantTests()
    {
        _mockHumanInTheLoop = new Mock<IHumanInTheLoop>();
        _kernelStore = new KernelStore(Mock.Of<IServiceProvider>());
        _excelAssistant = new ExcelAssistant(_kernelStore, _mockHumanInTheLoop.Object);
    }

    [Fact]
    public void ExcelAssistant_ShouldInitializeWithCorrectName()
    {
        // Assert
        _excelAssistant.Name.Should().Be("ExcelAssistant");
    }

    [Fact]
    public async Task ExcelAssistant_ShouldHaveExportDatasetFunction()
    {
        // Act
        var functions = await _excelAssistant.GetFunctionsAsync();

        // Assert
        functions.Should().HaveCount(1);
        var exportFunction = functions.First();
        exportFunction.Name.Should().Be("ExportDataset");
        exportFunction.IsFinal.Should().BeFalse();
    }

}