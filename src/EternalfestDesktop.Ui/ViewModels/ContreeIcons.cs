using Avalonia.Media;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Ui.ViewModels;

/// <summary>Turns a contrée icon into an image the views can show.</summary>
public interface ContreeIcons
{
    Task<IImage?> Load(Blob icon, CancellationToken cancellationToken);
}
