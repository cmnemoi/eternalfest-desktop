using System.ComponentModel;
using Avalonia.Controls;
using EternalfestDesktop.Ui.ViewModels;

namespace EternalfestDesktop.Ui.Views;

public sealed partial class ContreePageView : UserControl
{
    private ContreePageViewModel? _page;

    public ContreePageView() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_page is not null)
            _page.PropertyChanged -= BringToFrontOnGameSummary;
        _page = DataContext as ContreePageViewModel;
        if (_page is not null)
            _page.PropertyChanged += BringToFrontOnGameSummary;
    }

    /// <summary>The game window just closed: the summary mustn't stay hidden behind another window.</summary>
    /// @spec ui::game-summary
    private void BringToFrontOnGameSummary(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ContreePageViewModel.GameSummary) && _page?.GameSummary is not null)
            (TopLevel.GetTopLevel(this) as Window)?.Activate();
    }
}
