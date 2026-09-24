using Avalonia.Media;
using Avalonia.Media.Imaging;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Ui.ViewModels;

namespace EternalfestDesktop.Ui;

internal sealed class BitmapContreeIcons(FetchIcon fetchIcon) : ContreeIcons
{
    public async Task<IImage?> Load(Blob icon, CancellationToken cancellationToken)
    {
        await using var stream = await fetchIcon.Execute(icon, cancellationToken);
        if (stream is null)
            return null;
        try
        {
            return new Bitmap(stream);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            // Some authors publish icons Skia can't decode: the library shows the contrée without one
            return null;
        }
    }
}
