# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## General rules

- Always comments interfaces, in classes use the inherits doc when needed.
- Try to write unit test for all modification if possible and if you do not need to change too many thing for testing, unless the user asked explicitly to refactor for testing.

## Development Commands

### Building and Testing
- **Build solution**: `dotnet build src/SemanticKernel.Orchestration.sln --configuration release`
- **Run tests**: `dotnet test src/SemanticKernel.Orchestration.Tests/SemanticKernel.Orchestration.Tests.csproj --no-restore`
- **Run single test**: `dotnet test src/SemanticKernel.Orchestration.Tests/SemanticKernel.Orchestration.Tests.csproj --filter "TestMethodName"`
- **Restore packages**: `dotnet restore src/SemanticKernel.Orchestration.sln`
- **Full build with tests**: Run `./build.ps1` (PowerShell script that handles restore, test, build, and packaging)

### Package Management
- Uses Central Package Management with `Directory.Packages.props`
- **Create NuGet package**: `dotnet pack src/SemanticKernel.Orchestration/SemanticKernel.Orchestration.csproj --configuration release`
- **Publish to NuGet**: `./build.ps1 -nugetApiKey "your-key" -nugetPublish $true`

### Development Tools
- **Restore tools**: `dotnet tool restore` (installs GitVersion and SonarScanner)
- **Get version**: `dotnet tool run dotnet-gitversion`

## Code Architecture

### Core Components

**Orchestration Model**: The library implements an opinionated orchestration pattern using Microsoft Semantic Kernel. The architecture follows a multi-layered approach:

**KernelStore** (`src/SemanticKernel.Orchestration/Orchestrators/KernelStore.cs`): Central registry for managing multiple Kernel instances. Each kernel has a unique name, model information, and description. Supports:
- Multiple kernel configurations for different AI models
- Global and per-kernel interceptors/wrappers
- Conversation context management with AsyncLocal storage
- Plugin registration per kernel

**Assistant Pattern**: Base class architecture for creating specialized assistants:
- **BaseAssistant**: Abstract base class providing function registration, property management, and conversation orchestration
- Assistants register functions that can be called by the orchestrator
- Support for function validation with `CanBeExecuted` predicates
- Property system for local and global state management

**Conversation Management**:
- **IConversation/BaseConversation**: Interface for managing chat history and message flow
- **TokenLimitedConversation**: Automatically manages token limits by truncating conversation history
- **SimpleConversation**: Basic in-memory conversation implementation

**Routing System**:
- **IKernelRouter**: Interface for selecting appropriate kernels based on conversation context
- **SimpleKernelRouter**: Basic implementation for kernel selection

### Key Architectural Patterns

**Function Registration Pattern**: Assistants register delegates that can be called by Semantic Kernel's function calling mechanism. Functions are registered with metadata including whether they terminate conversation flow.

**Interceptor/Wrapper System**: Chain of responsibility pattern for pre/post-processing of LLM calls:
- **IChatInterceptorTool**: For intercepting chat completions
- **IChatWrappingTool**: For wrapping chat completions
- Can be applied globally or per-kernel

**Conversation Context**: Uses AsyncLocal to maintain conversation state across async operations within the same logical thread.

### Testing Patterns

When mocking Semantic Kernel components, use the established pattern:
```csharp
var builder = Kernel.CreateBuilder();
var mocks = builder.Services.AddMockedLLM("gpt4o");
var kernelStore = new KernelStore();
kernelStore.AddKernel("gpt4o", builder, ModelInformation.GPT4O);
mocks.ChatCompletionMock.SetMockResponse("Hello, I'm here to help!");
```

## Project Structure

- **SemanticKernel.Orchestration**: Main library (.NET 9.0)
- **SemanticKernel.Orchestration.Tests**: Unit tests using xUnit, Moq, FluentAssertions
- **SemanticKernel.Orchestration.SampleAgents**: Example assistant implementations
- **SemanticKernel.Orchestration.ConsolePlayground**: Console application for testing

## Configuration

### Environment Variables
The library expects Azure OpenAI configuration through environment variables:
- `OPENAI_API_BASE`: Azure OpenAI endpoint
- `OPENAI_API_KEY`: Azure OpenAI API key

### .NET Configuration
- Target Framework: .NET 9.0
- Nullable reference types enabled
- Latest C# language version
- XML documentation generation enabled
- Warning CS1591 (missing XML comments) treated as message, not error