using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernel.Orchestration.Helpers;

/// <summary>
/// Implements the concept of a human in the loop, which is a system that allows for human intervention in the decision-making process.
/// </summary>
public interface IHumanInTheLoop
{
    /// <summary>
    /// This allow the agent/orchestrator to write a status message to the user. Eeach
    /// time a status is written the old status could be removed (it depends on the implementation).
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task <string> WriteStatusAsync(string message);

    /// <summary>
    /// Asks a question to the user and returns the answer in natural language format.
    /// </summary>
    /// <param name="question"></param>
    /// <returns></returns>
    Task<string> AskQuestionAsync(string question);

    /// <summary>
    /// Asks the user to select an option from a list of options and returns the selected option.
    /// This is useful for any scenario where the user needs to make a choice from a set of options.
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<string> AskForSelectionAsync(string prompt, IEnumerable<string> options);
}


