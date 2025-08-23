using System.Collections.Generic;

namespace SemanticKernel.Orchestration.Helpers;

public interface IConsoleWrapper
{
    void WriteLine(string message);
    string? ReadLine();
    string PromptSelection(string prompt, IEnumerable<string> options);
}