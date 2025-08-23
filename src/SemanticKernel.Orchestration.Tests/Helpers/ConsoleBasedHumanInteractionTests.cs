using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SemanticKernel.Orchestration.Helpers;
using Xunit;

namespace SemanticKernel.Orchestration.Tests.Helpers;

public class ConsoleBasedHumanInteractionTests
{
    [Fact]
    public async Task AskForMultiSelectionAsync_WithNumbers_ShouldReturnSelectedOptions()
    {
        // Arrange
        var mockConsole = new Mock<IConsoleWrapper>();
        var options = new[] { "Option A", "Option B", "Option C" };
        mockConsole.Setup(x => x.ReadLine()).Returns("1,3");
        
        var interaction = new ConsoleBasedHumanInteraction(mockConsole.Object);
        
        // Act
        var result = await interaction.AskForMultiSelectionAsync("Select options:", options);
        
        // Assert
        result.Should().BeEquivalentTo(new[] { "Option A", "Option C" });
    }
    
    [Fact]
    public async Task AskForMultiSelectionAsync_WithNames_ShouldReturnSelectedOptions()
    {
        // Arrange
        var mockConsole = new Mock<IConsoleWrapper>();
        var options = new[] { "Option A", "Option B", "Option C" };
        mockConsole.Setup(x => x.ReadLine()).Returns("Option A, Option C");
        
        var interaction = new ConsoleBasedHumanInteraction(mockConsole.Object);
        
        // Act
        var result = await interaction.AskForMultiSelectionAsync("Select options:", options);
        
        // Assert
        result.Should().BeEquivalentTo(new[] { "Option A", "Option C" });
    }
    
    [Fact]
    public async Task AskForMultiSelectionAsync_WithMixedInput_ShouldReturnSelectedOptions()
    {
        // Arrange
        var mockConsole = new Mock<IConsoleWrapper>();
        var options = new[] { "Option A", "Option B", "Option C", "Option D" };
        mockConsole.Setup(x => x.ReadLine()).Returns("1, Option C, 4");
        
        var interaction = new ConsoleBasedHumanInteraction(mockConsole.Object);
        
        // Act
        var result = await interaction.AskForMultiSelectionAsync("Select options:", options);
        
        // Assert
        result.Should().BeEquivalentTo(new[] { "Option A", "Option C", "Option D" });
    }
    
    [Fact]
    public async Task AskForMultiSelectionAsync_WithCaseInsensitiveNames_ShouldReturnSelectedOptions()
    {
        // Arrange
        var mockConsole = new Mock<IConsoleWrapper>();
        var options = new[] { "Option A", "Option B", "Option C" };
        mockConsole.Setup(x => x.ReadLine()).Returns("option a, OPTION C");
        
        var interaction = new ConsoleBasedHumanInteraction(mockConsole.Object);
        
        // Act
        var result = await interaction.AskForMultiSelectionAsync("Select options:", options);
        
        // Assert
        result.Should().BeEquivalentTo(new[] { "Option A", "Option C" });
    }
    
    [Fact]
    public async Task AskForMultiSelectionAsync_WithInvalidInput_ShouldIgnoreInvalidSelections()
    {
        // Arrange
        var mockConsole = new Mock<IConsoleWrapper>();
        var options = new[] { "Option A", "Option B", "Option C" };
        mockConsole.Setup(x => x.ReadLine()).Returns("1, Invalid Option, 99, Option B");
        
        var interaction = new ConsoleBasedHumanInteraction(mockConsole.Object);
        
        // Act
        var result = await interaction.AskForMultiSelectionAsync("Select options:", options);
        
        // Assert
        result.Should().BeEquivalentTo(new[] { "Option A", "Option B" });
    }
    
    [Fact]
    public async Task AskForMultiSelectionAsync_WithEmptyInput_ShouldReturnEmpty()
    {
        // Arrange
        var mockConsole = new Mock<IConsoleWrapper>();
        var options = new[] { "Option A", "Option B", "Option C" };
        mockConsole.Setup(x => x.ReadLine()).Returns("");
        
        var interaction = new ConsoleBasedHumanInteraction(mockConsole.Object);
        
        // Act
        var result = await interaction.AskForMultiSelectionAsync("Select options:", options);
        
        // Assert
        result.Should().BeEmpty();
    }
    
    [Fact]
    public async Task AskForMultiSelectionAsync_WithDuplicateSelections_ShouldReturnDistinctOptions()
    {
        // Arrange
        var mockConsole = new Mock<IConsoleWrapper>();
        var options = new[] { "Option A", "Option B", "Option C" };
        mockConsole.Setup(x => x.ReadLine()).Returns("1, Option A, 1");
        
        var interaction = new ConsoleBasedHumanInteraction(mockConsole.Object);
        
        // Act
        var result = await interaction.AskForMultiSelectionAsync("Select options:", options);
        
        // Assert
        result.Should().BeEquivalentTo(new[] { "Option A" });
        result.Count().Should().Be(1);
    }
    
    [Fact]
    public async Task AskQuestionAsync_ShouldUseConsoleWrapper()
    {
        // Arrange
        var mockConsole = new Mock<IConsoleWrapper>();
        mockConsole.Setup(x => x.ReadLine()).Returns("Test Answer");
        
        var interaction = new ConsoleBasedHumanInteraction(mockConsole.Object);
        
        // Act
        var result = await interaction.AskQuestionAsync("Test question?");
        
        // Assert
        result.Should().Be("Test Answer");
        mockConsole.Verify(x => x.WriteLine("Test question?"), Times.Once);
        mockConsole.Verify(x => x.ReadLine(), Times.Once);
    }
}