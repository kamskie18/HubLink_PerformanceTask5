using System.Threading.Tasks;

namespace HubLink.Desktop.Services;

public interface IFileDialogService
{
    Task<string?> PickCsvSavePathAsync(string suggestedFileName);
}
