using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Ui.Resources;

namespace EternalfestDesktop.Ui.ViewModels;

/// <summary>A contrée's page: what the player chooses before playing it.</summary>
/// @spec ui::contree-page
public sealed partial class ContreePageViewModel(
    ContreeCardViewModel contree,
    GameCatalog catalog,
    GameStore store,
    PlayGame playGame,
    Version bundledLoader,
    Action back) : ObservableObject
{
    public ContreeCardViewModel Contree { get; } = contree;
    public ObservableCollection<ModeViewModel> Modes { get; } = [];
    public ObservableCollection<LocaleViewModel> Locales { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    public partial ModeViewModel? SelectedMode { get; set; }

    [ObservableProperty]
    public partial LocaleViewModel? SelectedLocale { get; set; }

    [ObservableProperty]
    public partial int Volume { get; set; } = 100;

    [ObservableProperty]
    public partial bool Fullscreen { get; set; }

    [ObservableProperty]
    public partial string? LoaderWarning { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? Status { get; set; }

    [ObservableProperty]
    public partial bool IsDownloading { get; set; }

    [RelayCommand]
    private void Back() => back();

    public async Task Load(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        Game game;
        try
        {
            game = await store.FindGame(Contree.Id, cancellationToken) ?? await catalog.GetGame(Contree.Id, cancellationToken);
        }
        catch (Exception exception) when (exception is EternalfestUnreachableException or GameNotFoundException)
        {
            ErrorMessage = Text.ErrorMessage(exception);
            return;
        }

        Modes.Clear();
        foreach (var mode in game.Build.WithFullOptions().Modes.Where(mode => mode.IsVisible))
            Modes.Add(new ModeViewModel(mode));
        SelectedMode = Modes.FirstOrDefault();

        Locales.Clear();
        foreach (var locale in Enumerable.Concat([game.Build.MainLocale], game.Build.LocalizedContent.Keys).Distinct())
            Locales.Add(new LocaleViewModel(locale));
        SelectedLocale = Locales.FirstOrDefault(locale => locale.Code.StartsWith(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName + "-", StringComparison.OrdinalIgnoreCase))
            ?? Locales.FirstOrDefault();

        // @spec play::warns-newer-loader
        LoaderWarning = game.Build.RequiresNewerLoaderThan(bundledLoader)
            ? Text.Format(Strings.NewerLoaderWarning, game.Build.LoaderVersion, bundledLoader.ToString(3))
            : null;
        IsLoaded = true;
    }

    /// @spec ui::download-progress
    [RelayCommand(CanExecute = nameof(IsLoaded), IncludeCancelCommand = true)]
    private async Task Play(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        Status = Strings.Playing;
        var choices = new RunChoices(
            SelectedMode?.Key,
            SelectedMode?.Options.Where(option => option.IsChecked).Select(option => option.Key).ToList(),
            SelectedLocale?.Code,
            Volume,
            Fullscreen);
        try
        {
            await playGame.Execute(Contree.Id, choices, new Progress<DownloadProgress>(ReportDownload), cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            ErrorMessage = Text.ErrorMessage(exception);
        }
        finally
        {
            Status = null;
            IsDownloading = false;
            Contree.IsDownloaded = await store.FindGame(Contree.Id, CancellationToken.None) is not null;
        }
    }

    private void ReportDownload(DownloadProgress progress)
    {
        IsDownloading = progress.DownloadedBytes < progress.TotalBytes;
        Status = IsDownloading
            ? Text.Format(Strings.Downloading, progress.TotalBytes == 0 ? 100 : progress.DownloadedBytes * 100 / progress.TotalBytes)
            : Strings.Playing;
    }
}

public sealed class ModeViewModel(GameMode mode)
{
    public string Key { get; } = mode.Key;
    public string DisplayName { get; } = mode.DisplayName;

    public IReadOnlyList<OptionViewModel> Options { get; } = mode.Options
        .Where(option => option.IsVisible)
        .Select(option => new OptionViewModel(option.Key, option.DisplayName) { IsChecked = option.DefaultValue })
        .ToList();

    public bool HasNoOption => Options.Count == 0;
}

public sealed partial class OptionViewModel(string key, string displayName) : ObservableObject
{
    public string Key { get; } = key;
    public string DisplayName { get; } = displayName;

    [ObservableProperty]
    public partial bool IsChecked { get; set; }
}

public sealed class LocaleViewModel(string code)
{
    public string Code { get; } = code;

    public string DisplayName { get; } = NameOf(code);

    private static string NameOf(string code)
    {
        try
        {
            var name = CultureInfo.GetCultureInfo(code).NativeName;
            return string.IsNullOrEmpty(name) ? code : char.ToUpper(name[0], CultureInfo.CurrentCulture) + name[1..];
        }
        catch (CultureNotFoundException)
        {
            return code;
        }
    }
}
