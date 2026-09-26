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
    DownloadGame downloadGame,
    PlayGame playGame,
    QuestBook quests,
    Version bundledLoader,
    PreferencesFile preferences,
    Func<Task> back) : ObservableObject
{
    public ContreeCardViewModel Contree { get; } = contree;
    public ObservableCollection<ModeViewModel> Modes { get; } = [];
    public ObservableCollection<LocaleViewModel> Locales { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
    public partial bool IsLoaded { get; set; }

    private Game? _game;

    /// @spec ui::profile-picker
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompleteProfile), nameof(IsNewPlayer))]
    public partial PlayerProfile Profile { get; set; }

    /// <summary>For the "Complete profile" radio button: unchecking it is checking the other one.</summary>
    public bool IsCompleteProfile
    {
        get => Profile == PlayerProfile.Complete;
        set
        {
            if (value)
                Profile = PlayerProfile.Complete;
        }
    }

    /// <summary>For the "New player" radio button: unchecking it is checking the other one.</summary>
    public bool IsNewPlayer
    {
        get => Profile == PlayerProfile.NewPlayer;
        set
        {
            if (value)
                Profile = PlayerProfile.NewPlayer;
        }
    }

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

    [ObservableProperty]
    public partial GameSummaryViewModel? GameSummary { get; set; }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task Back() => back();

    /// <summary>Downloads the contrée's newer build; the downloaded one stays playable if that fails.</summary>
    /// @spec store::detects-newer-build
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Update(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        try
        {
            await downloadGame.Update(Contree.Id, new Progress<DownloadProgress>(ReportDownload), cancellationToken);
            Contree.IsUpdateAvailable = false;
            await Load(cancellationToken);
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
        }
    }

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

        _game = game;
        Profile = PlayerProfile.Complete;
        ShowModes();

        Locales.Clear();
        foreach (var locale in Enumerable.Concat([game.Build.MainLocale], game.Build.LocalizedContent.Keys).Distinct())
            Locales.Add(new LocaleViewModel(locale));
        SelectedLocale = Locales.FirstOrDefault(locale => locale.Code.StartsWith(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName + "-", StringComparison.OrdinalIgnoreCase))
            ?? Locales.FirstOrDefault();

        Remembered(preferences.Current.Choices.GetValueOrDefault(Contree.Id.Value));

        // @spec play::warns-newer-loader
        LoaderWarning = game.Build.RequiresNewerLoaderThan(bundledLoader)
            ? Text.Format(Strings.NewerLoaderWarning, game.Build.LoaderVersion, bundledLoader.ToString(3))
            : null;
        IsLoaded = true;
    }

    /// <summary>Offers the modes and options the profile unlocks, keeping the selected ones still offered.</summary>
    /// @spec ui::profile-picker
    private void ShowModes()
    {
        if (_game is null)
            return;
        var previous = SelectedMode;
        var progression = quests.ProgressionOf(_game.Key);
        // What a profile unlocks doesn't depend on the items of the content, so it isn't read here
        var unlocked = Player.For(Profile, progression, contentItems: []).Unlocks(_game, progression);
        Modes.Clear();
        foreach (var mode in unlocked.Build.WithFullOptions().Modes.Where(mode => mode.IsVisible))
            Modes.Add(new ModeViewModel(mode));
        SelectedMode = Modes.FirstOrDefault(mode => mode.Key == previous?.Key) ?? Modes.FirstOrDefault();
        if (previous is null || SelectedMode?.Key != previous.Key)
            return;
        foreach (var option in SelectedMode.Options)
            option.IsChecked = previous.Options.FirstOrDefault(kept => kept.Key == option.Key)?.IsChecked ?? option.IsChecked;
    }

    partial void OnProfileChanged(PlayerProfile value) => ShowModes();

    /// <summary>Selects what the player chose last time, when the contrée still offers it.</summary>
    /// @spec ui::contree-page
    private void Remembered(RunChoices? choices)
    {
        if (choices is null)
            return;
        Profile = choices.Profile;
        if (Modes.FirstOrDefault(mode => mode.Key == choices.Mode) is { } mode)
        {
            SelectedMode = mode;
            foreach (var option in mode.Options)
                option.IsChecked = choices.Options?.Contains(option.Key) ?? option.IsChecked;
        }
        SelectedLocale = Locales.FirstOrDefault(locale => locale.Code == choices.Locale) ?? SelectedLocale;
        Volume = choices.Volume;
        Fullscreen = choices.Fullscreen;
    }

    /// @spec ui::download-progress
    [RelayCommand(CanExecute = nameof(IsLoaded), IncludeCancelCommand = true)]
    private async Task Play(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        GameSummary = null;
        Status = Strings.Playing;
        var choices = new RunChoices(
            SelectedMode?.Key,
            SelectedMode?.Options.Where(option => option.IsChecked).Select(option => option.Key).ToList(),
            SelectedLocale?.Code,
            Volume,
            Fullscreen,
            Profile);
        // @spec ui::play-again
        preferences.Current.Choices[Contree.Id.Value] = choices;
        preferences.Current.LastPlayed = Contree.Id.Value;
        preferences.Save();
        try
        {
            // @spec ui::game-summary
            if (await playGame.Execute(Contree.Id, choices, new Progress<DownloadProgress>(ReportDownload), cancellationToken) is { } result)
                GameSummary = new GameSummaryViewModel(result);
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

    /// <summary>Replays the game that just ended: its choices are the ones remembered for the contrée.</summary>
    /// @spec ui::game-summary
    [RelayCommand]
    private Task PlayAgain()
    {
        Remembered(preferences.Current.Choices.GetValueOrDefault(Contree.Id.Value));
        return PlayCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void CloseGameSummary() => GameSummary = null;

    private void ReportDownload(DownloadProgress progress)
    {
        IsDownloading = progress.DownloadedBytes < progress.TotalBytes;
        Status = IsDownloading || !PlayCommand.IsRunning
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
