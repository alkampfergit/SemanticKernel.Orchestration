using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace SemanticKernel.Orchestration.Helpers;

public class SpectreConsoleWrapper : IConsoleWrapper
{
    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    public string? ReadLine()
    {
        return Console.ReadLine();
    }

    public string PromptSelection(string prompt, IEnumerable<string> options)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .PageSize(20)
                .MoreChoicesText("[grey](Move up and down to select the option)[/]")
                .AddChoices(options.ToArray()));
    }
}