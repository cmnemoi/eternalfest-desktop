using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Ui.ViewModels;

/// <summary>A contrée in the library.</summary>
public sealed partial class ContreeCardViewModel(GameId id, string displayName, string description, string version, Blob? icon) : ObservableObject
{
    public GameId Id { get; } = id;
    public string DisplayName { get; } = displayName;
    public string Description { get; } = description;
    public string Version { get; } = version;
    public Blob? IconBlob { get; } = icon;

    [ObservableProperty]
    public partial IImage? Icon { get; set; }

    /// @spec ui::library
    [ObservableProperty]
    public partial bool IsDownloaded { get; set; }

    /// @spec store::detects-newer-build
    [ObservableProperty]
    public partial bool IsUpdateAvailable { get; set; }

    internal string SearchableText { get; } = Text.Searchable($"{displayName} {description}");
}
