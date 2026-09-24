using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EternalfestDesktop.Application;
using EternalfestDesktop.Ui.Resources;

namespace EternalfestDesktop.Ui.ViewModels;

/// <summary>Launcher settings. Language and cache folder changes apply at the next start.</summary>
/// @spec ui::settings
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly PreferencesFile _preferences;
    private readonly ClearCache _clearCache;
    private readonly string _logsFolder;
    private readonly Action _back;

    public SettingsViewModel(PreferencesFile preferences, ClearCache clearCache, string cacheFolder, string logsFolder, Action back)
    {
        _preferences = preferences;
        _clearCache = clearCache;
        _logsFolder = logsFolder;
        _back = back;
        SelectedLanguage = Languages.First(language => language.Code == preferences.Current.UiLanguage);
        CacheFolder = cacheFolder;
    }

    public IReadOnlyList<LanguageViewModel> Languages { get; } =
    [
        new(null, Strings.SystemLanguage),
        new("en", "English"),
        new("fr", "Français"),
    ];

    [ObservableProperty]
    public partial LanguageViewModel SelectedLanguage { get; set; }

    [ObservableProperty]
    public partial string CacheFolder { get; set; }

    [ObservableProperty]
    public partial bool IsRestartNeeded { get; set; }

    [ObservableProperty]
    public partial string? Message { get; set; }

    partial void OnSelectedLanguageChanged(LanguageViewModel value)
    {
        if (_preferences.Current.UiLanguage == value.Code)
            return;
        _preferences.Current.UiLanguage = value.Code;
        _preferences.Save();
        IsRestartNeeded = true;
    }

    /// <summary>Called with the folder the player picked.</summary>
    public void ChangeCacheFolder(string folder)
    {
        CacheFolder = folder;
        _preferences.Current.CacheFolder = folder;
        _preferences.Save();
        IsRestartNeeded = true;
    }

    [RelayCommand]
    private async Task ClearCache(CancellationToken cancellationToken)
    {
        try
        {
            await _clearCache.Execute(cancellationToken);
            Message = Strings.CacheCleared;
        }
        catch (Exception exception) when (exception is GameAlreadyRunningException or IOException or UnauthorizedAccessException)
        {
            Message = Text.ErrorMessage(exception);
        }
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        Directory.CreateDirectory(_logsFolder);
        Process.Start(new ProcessStartInfo(_logsFolder) { UseShellExecute = true });
    }

    [RelayCommand]
    private void Back() => _back();
}

public sealed record LanguageViewModel(string? Code, string DisplayName);
