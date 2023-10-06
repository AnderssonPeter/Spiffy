using System.Text;

using Spiffy.Core.Contracts.Services;
using Spiffy.Core.Helpers;

namespace Spiffy.Core.Services;

public class FileService : IFileService
{
    public async Task<T> ReadAsync<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = await File.ReadAllTextAsync(path);
            return Json.ToObject<T>(json);
        }

        return default;
    }

    public async Task SaveAsync<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = Json.Stringify(content);
        await File.WriteAllTextAsync(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
    }

    public void Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }
}
