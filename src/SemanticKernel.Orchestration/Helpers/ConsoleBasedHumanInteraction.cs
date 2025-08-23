using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Helpers;

public class ConsoleBasedHumanInteraction : IHumanInTheLoop
{
    private readonly IConsoleWrapper _consoleWrapper;

    public ConsoleBasedHumanInteraction(IConsoleWrapper? consoleWrapper = null)
    {
        _consoleWrapper = consoleWrapper ?? new SpectreConsoleWrapper();
    }

    public Task<string> AskQuestionAsync(string question)
    {
        _consoleWrapper.WriteLine(question);
        return Task.FromResult(_consoleWrapper.ReadLine() ?? string.Empty);
    }

    public Task<string> AskForSelectionAsync(string prompt, IEnumerable<string> options)
    {
        var selection = _consoleWrapper.PromptSelection(prompt, options);
        return Task.FromResult(selection);
    }

    public Task<IEnumerable<string>> AskForMultiSelectionAsync(string prompt, IEnumerable<string> options)
    {
        var optionsList = options.ToList();
        _consoleWrapper.WriteLine($"{prompt}");
        _consoleWrapper.WriteLine("Available options:");
        
        for (int i = 0; i < optionsList.Count; i++)
        {
            _consoleWrapper.WriteLine($"{i + 1}. {optionsList[i]}");
        }
        
        _consoleWrapper.WriteLine("Enter your selections (comma-separated numbers or option names):");
        var input = _consoleWrapper.ReadLine() ?? string.Empty;
        
        return Task.FromResult(ParseMultiSelection(input, optionsList));
    }

    public Task<string> WriteStatusAsync(string message)
    {
        _consoleWrapper.WriteLine(message);
        return Task.FromResult(message);
    }

    private IEnumerable<string> ParseMultiSelection(string input, List<string> options)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Enumerable.Empty<string>();
        }

        var selections = new List<string>();
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            
            // Try to parse as number (1-based index)
            if (int.TryParse(trimmedPart, out int index) && index >= 1 && index <= options.Count)
            {
                selections.Add(options[index - 1]);
            }
            // Try to find exact match by name (case-insensitive)
            else
            {
                var matchingOption = options.FirstOrDefault(o => 
                    string.Equals(o, trimmedPart, StringComparison.OrdinalIgnoreCase));
                if (matchingOption != null)
                {
                    selections.Add(matchingOption);
                }
            }
        }

        return selections.Distinct();
    }
}
