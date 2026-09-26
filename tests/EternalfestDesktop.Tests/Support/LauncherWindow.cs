using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EternalfestDesktop.Ui.ViewModels;
using EternalfestDesktop.Ui.Views;

namespace EternalfestDesktop.Tests.Support;

/// <summary>
/// The launcher's window as the player sees it: it only reads what is shown, and only clicks and types where the player could.
/// A hidden, disabled or covered button can't be clicked.
/// </summary>
internal sealed class LauncherWindow
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);
    private readonly Window _window;

    private LauncherWindow(Window window) => _window = window;

    /// <summary>Opens the window on the library, as the app does when it starts.</summary>
    public static LauncherWindow Opening(MainWindowViewModel main)
    {
        var window = new MainWindow { DataContext = main };
        window.Show();
        _ = main.Library.Load(CancellationToken.None);
        return new LauncherWindow(window);
    }

    public MainWindowViewModel Main => (MainWindowViewModel)_window.DataContext!;

    /// <summary>Every text the player can see, in reading order.</summary>
    public IReadOnlyList<string> Texts
    {
        get
        {
            Render();
            return [.. _window.GetVisualDescendants().OfType<TextBlock>().Where(IsShown).Select(text => text.Text ?? "").Where(text => text.Length > 0)];
        }
    }

    public bool Shows(string text) => Texts.Any(shown => shown.Contains(text, StringComparison.Ordinal));

    /// <summary>The names on the library's cards, in order.</summary>
    public IReadOnlyList<string> Cards
    {
        get
        {
            Render();
            return [.. Shown<Button>()
                .Where(button => button.DataContext is ContreeCardViewModel)
                .Select(button => ((ContreeCardViewModel)button.DataContext!).DisplayName)];
        }
    }

    /// <summary>The label of every button the views declare and show, cards aside.</summary>
    public IReadOnlyList<string> Buttons
    {
        get
        {
            Render();
            return [.. Shown<Button>()
                .Where(button => button.TemplatedParent is null && button.DataContext is not ContreeCardViewModel)
                .Select(button => string.Concat(button.GetVisualDescendants().OfType<TextBlock>().Where(IsShown).Select(text => text.Text)))];
        }
    }

    /// <summary>What the card of <paramref name="contree"/> shows: its name, description and badges.</summary>
    public IReadOnlyList<string> TextsOnCard(string contree) =>
        [.. Card(contree).GetVisualDescendants().OfType<TextBlock>().Where(IsShown).Select(text => text.Text ?? "").Where(text => text.Length > 0)];

    public bool ShowsIconOn(string contree) => Card(contree).GetVisualDescendants().OfType<Image>().Any(image => IsShown(image) && image.Source is not null);

    private Button Card(string contree)
    {
        Render();
        return Shown<Button>().SingleOrDefault(button => button.DataContext is ContreeCardViewModel card && card.DisplayName == contree)
            ?? throw new InvalidOperationException($"No card for \"{contree}\" among: {string.Join(" | ", Cards)}");
    }

    /// <summary>Waits for the app to show <paramref name="text"/>, as the player would.</summary>
    public Task WaitFor(string text) => WaitUntil(() => Shows(text), $"\"{text}\" to be shown, among: {string.Join(" | ", Texts)}");

    public Task WaitUntilGone(string text) => WaitUntil(() => !Shows(text), $"\"{text}\" to disappear");

    public async Task WaitUntil(Func<bool> condition, string awaited)
    {
        var deadline = DateTime.UtcNow + Patience;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                Assert.Fail($"Waited {Patience.TotalSeconds}s for {awaited}");
            await Task.Delay(10, TestContext.Current.CancellationToken);
            Render();
        }
    }

    /// <summary>Clicks the button, check box or card that shows <paramref name="text"/>, with the mouse.</summary>
    public void Click(string text)
    {
        var target = Clickable(text);
        target.BringIntoView();
        Render();
        var center = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), _window)
            ?? throw new InvalidOperationException($"\"{text}\" isn't in the window.");
        _window.MouseDown(center, MouseButton.Left);
        _window.MouseUp(center, MouseButton.Left);
        Render();
    }

    /// <summary>Clicks in the text box whose placeholder is <paramref name="placeholder"/>, then types <paramref name="text"/>.</summary>
    public void Type(string placeholder, string text)
    {
        var box = Shown<TextBox>().Single(box => box.PlaceholderText == placeholder);
        box.Focus();
        _window.KeyTextInput(text);
        Render();
    }

    /// <summary>Picks <paramref name="item"/> in the drop-down list offering it, from the keyboard.</summary>
    public void Choose(string item)
    {
        var list = Shown<ComboBox>().Single(list => list.Items.Any(offered => DisplayNameOf(offered) == item));
        var from = list.SelectedIndex;
        var to = list.Items.Cast<object?>().Select(DisplayNameOf).ToList().IndexOf(item);
        list.Focus();
        for (var step = from; step != to; step += Math.Sign(to - from))
            _window.KeyPressQwerty(to > from ? PhysicalKey.ArrowDown : PhysicalKey.ArrowUp, RawInputModifiers.None);
        Render();
    }

    public bool IsChecked(string text) => Clickable(text) is Avalonia.Controls.Primitives.ToggleButton { IsChecked: true };

    private Control Clickable(string text)
    {
        Render();
        var label = _window.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(label => label.Text == text && IsShown(label))
            ?? throw new InvalidOperationException($"No \"{text}\" among: {string.Join(" | ", Texts)}");
        var clickable = label.GetVisualAncestors().OfType<Avalonia.Controls.Primitives.ToggleButton>().Cast<Control>()
            .Concat(label.GetVisualAncestors().OfType<Button>())
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"\"{text}\" can't be clicked.");
        return clickable;
    }

    private IEnumerable<T> Shown<T>() where T : Visual => _window.GetVisualDescendants().OfType<T>().Where(IsShown);

    private static bool IsShown(Visual visual) => visual.IsEffectivelyVisible && visual.Opacity > 0;

    private static string? DisplayNameOf(object? item) => item?.GetType().GetProperty("DisplayName")?.GetValue(item) as string;

    private void Render()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        _window.UpdateLayout();
    }
}
