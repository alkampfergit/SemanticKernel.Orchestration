using Microsoft.SemanticKernel;
using OfficeOpenXml;
using SemanticKernel.Orchestration.Assistants;
using SemanticKernel.Orchestration.Helpers;
using SemanticKernel.Orchestration.Orchestrators;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;

namespace SemanticKernel.Orchestration.SampleAgents.Common;

/// <summary>
/// Assistant for exporting query results to Excel files
/// </summary>
public class ExcelAssistant : BaseAssistant
{
    private readonly KernelStore _kernelStore;
    private readonly IHumanInTheLoop _humanInTheLoop;

    public ExcelAssistant(
        KernelStore kernelStore,
        IHumanInTheLoop humanInTheLoop
    ) : base("ExcelAssistant")
    {
        RegisterFunctionDelegate(
            "ExportDataset",
            KernelFunctionFactory.CreateFromMethod(ExportDataset),
            async (args) => await ExportDataset(),
            isFinal: false);
        _kernelStore = kernelStore;
        _humanInTheLoop = humanInTheLoop;
    }

    [Description("Export a dataset to an excel file")]
    private async Task<AssistantResponse> ExportDataset()
    {
        var datasets = KernelStore.GetAllPropertyValues<DataSet>();
        if (datasets.Count == 0)
        {
            return new AssistantResponse("No dataset found in the current conversation");
        }

        // Get the datasets to export based on user selection
        var datasetsToExport = await GetDatasetsToExport(datasets);
        if (!datasetsToExport.Any())
        {
            return new AssistantResponse("No datasets selected for export");
        }

        using var package = new ExcelPackage();
        var tempFilePath = Path.Combine(Path.GetTempPath(), "exported_dataset.xlsx");
        
        foreach (var dataset in datasetsToExport)
        {
            var prefix = dataset.Key;
            foreach (DataTable table in dataset.Value.Tables)
            {
                var worksheetName = $"{prefix}_{table.TableName}";
                var worksheet = package.Workbook.Worksheets.Add(worksheetName);
                worksheet.Cells["A1"].LoadFromDataTable(table, true);
            }
        }

        await package.SaveAsAsync(new FileInfo(tempFilePath));

        Process.Start(new ProcessStartInfo(tempFilePath) { UseShellExecute = true });
        
        var exportedNames = string.Join(", ", datasetsToExport.Select(d => d.Key));
        return new AssistantResponse($"Dataset(s) exported successfully: {exportedNames}. File saved to: {tempFilePath}");
    }

    /// <summary>
    /// Determines which datasets to export based on the number of available datasets
    /// </summary>
    /// <param name="datasets">All available datasets</param>
    /// <returns>The datasets selected for export</returns>
    private async Task<IEnumerable<(string Key, DataSet Value)>> GetDatasetsToExport(
        IReadOnlyCollection<(string Key, DataSet Value)> datasets)
    {
        // If only one dataset, export it automatically
        if (datasets.Count == 1)
        {
            return datasets;
        }

        // Multiple datasets - let user choose
        var options = new List<string> { "All" };
        options.AddRange(datasets.Select(d => d.Key));

        var selectedOptions = await _humanInTheLoop.AskForMultiSelectionAsync(
            "Multiple query results found. Which ones would you like to export?",
            options);

        var selectedList = selectedOptions.ToList();
        
        // If "All" is selected or no specific selection, return all datasets
        if (!selectedList.Any() || selectedList.Contains("All", StringComparer.OrdinalIgnoreCase))
        {
            return datasets;
        }

        // Return only the selected datasets
        return datasets.Where(d => selectedList.Contains(d.Key, StringComparer.OrdinalIgnoreCase));
    }
}
