using Microsoft.Extensions.Options;

using Spiffy.Contracts.Services;
using Spiffy.Core.Contracts.Services;
using Spiffy.Core.Helpers;
using Spiffy.Helpers;
using Spiffy.Models;

using Windows.ApplicationModel;
using Windows.Storage;

namespace Spiffy.Services;

public class LocalSettingsService : ILocalSettingsService
{
    private const string defaultApplicationDataFolder = "Spiffy/ApplicationData";
    private const string defaultLocalSettingsFile = "LocalSettings.json";

    private readonly IFileService fileService;
    private readonly LocalSettingsOptions options;

    private readonly string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string applicationDataFolder;
    private readonly string localsettingsFile;

    private IDictionary<string, object> settings;

    private bool isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        this.fileService = fileService;
        this.options = options.Value;

        applicationDataFolder = Path.Combine(localApplicationData, this.options.ApplicationDataFolder ?? defaultApplicationDataFolder);
        localsettingsFile = this.options.LocalSettingsFile ?? defaultLocalSettingsFile;

        settings = new Dictionary<string, object>();
    }

    private async Task InitializeAsync()
    {
        if (!isInitialized)
        {
            settings = await fileService.ReadAsync<IDictionary<string, object>>(applicationDataFolder, localsettingsFile) ?? new Dictionary<string, object>();

            isInitialized = true;
        }
    }

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
            {
                return Json.ToObject<T>((string)obj);
            }
        }
        else
        {
            await InitializeAsync();

            if (settings != null && settings.TryGetValue(key, out var obj))
            {
                return Json.ToObject<T>((string)obj);
            }
        }

        return default;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = Json.Stringify(value);
        }
        else
        {
            await InitializeAsync();

            settings[key] = Json.Stringify(value);

            await fileService.SaveAsync(applicationDataFolder, localsettingsFile, settings);
        }
    }
}
