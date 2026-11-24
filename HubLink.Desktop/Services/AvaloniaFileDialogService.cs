using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace HubLink.Desktop.Services;

public class AvaloniaFileDialogService : IFileDialogService
{
    public async Task<string?> PickCsvSavePathAsync(string suggestedFileName)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } storageProvider)
        {
            return null;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Shipments to CSV",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "csv",
            ShowOverwritePrompt = true,
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("CSV Files")
                {
                    Patterns = new[] { "*.csv" },
                    MimeTypes = new[] { "text/csv" }
                }
            }
        });

        return file?.TryGetLocalPath() ?? file?.Path.LocalPath;
    }
}
