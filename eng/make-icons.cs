// Makes the app icon from the Eternalfest logo (assets/icon/eternalfest-logo.png): a rounded tile, as
// src/EternalfestDesktop.Ui/Assets/icon.png for the window and icon.ico for the Windows executable.
// Usage: mise run icons
#:property ManagePackageVersionsCentrally=false
#:package SkiaSharp@3.119.4
#:package SkiaSharp.NativeAssets.Linux@3.119.4
using SkiaSharp;

var root = Path.GetFullPath(Path.Combine(AppContext.GetData("EntryPointFileDirectoryPath") as string ?? "eng", ".."));
var assets = Path.Combine(root, "src", "EternalfestDesktop.Ui", "Assets");
using var logo = SKBitmap.Decode(Path.Combine(root, "assets", "icon", "eternalfest-logo.png"));

// At small sizes the rays around the triangle are noise: the tile zooms in on the triangle instead.
const int SmallSize = 32;
var triangle = RedBounds(logo);
var closeUp = SKRect.Inflate(Square(triangle), triangle.Width * 0.12f, triangle.Width * 0.12f);
var whole = SKRect.Create(logo.Width, logo.Height);

int[] sizes = [16, 24, 32, 48, 64, 128, 256];
var tiles = sizes.ToDictionary(size => size, size => Tile(logo, size <= SmallSize ? closeUp : whole, size));

Directory.CreateDirectory(assets);
File.WriteAllBytes(Path.Combine(assets, "icon.png"), tiles[256]);
File.WriteAllBytes(Path.Combine(assets, "icon.ico"), Ico(tiles));
Console.WriteLine($"Made icon.png and icon.ico ({string.Join(", ", sizes)} px) in {assets}.");

/// <summary>The part of <paramref name="source" /> in <paramref name="crop" />, as a PNG tile with rounded corners.</summary>
static byte[] Tile(SKBitmap source, SKRect crop, int size)
{
    using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
    var canvas = surface.Canvas;
    canvas.Clear(SKColors.Transparent);
    var cornerRadius = size * 0.2f;
    using (var corners = new SKPath())
    {
        corners.AddRoundRect(SKRect.Create(size, size), cornerRadius, cornerRadius);
        canvas.ClipPath(corners, antialias: true);
    }
    using var image = SKImage.FromBitmap(source);
    canvas.DrawImage(image, crop, SKRect.Create(size, size), new SKSamplingOptions(SKCubicResampler.Mitchell));
    using var snapshot = surface.Snapshot();
    using var png = snapshot.Encode(SKEncodedImageFormat.Png, 100);
    return png.ToArray();
}

/// <summary>Where the logo's red triangle is.</summary>
static SKRect RedBounds(SKBitmap logo)
{
    var (left, top, right, bottom) = (logo.Width, logo.Height, 0, 0);
    for (var y = 0; y < logo.Height; y++)
        for (var x = 0; x < logo.Width; x++)
        {
            var pixel = logo.GetPixel(x, y);
            if (pixel.Red > 150 && pixel.Green < 90 && pixel.Blue < 90)
                (left, top, right, bottom) = (Math.Min(left, x), Math.Min(top, y), Math.Max(right, x), Math.Max(bottom, y));
        }
    return new SKRect(left, top, right + 1, bottom + 1);
}

static SKRect Square(SKRect rect)
{
    var side = Math.Max(rect.Width, rect.Height);
    return SKRect.Create(rect.MidX - side / 2, rect.MidY - side / 2, side, side);
}

/// <summary>An .ico holding each size as a PNG, which Windows reads since Vista.</summary>
static byte[] Ico(IReadOnlyDictionary<int, byte[]> tiles)
{
    using var ico = new MemoryStream();
    using var writer = new BinaryWriter(ico);
    writer.Write((ushort)0);
    writer.Write((ushort)1);
    writer.Write((ushort)tiles.Count);
    var offset = 6 + 16 * tiles.Count;
    foreach (var (size, png) in tiles)
    {
        writer.Write((byte)(size % 256));
        writer.Write((byte)(size % 256));
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(png.Length);
        writer.Write(offset);
        offset += png.Length;
    }
    foreach (var png in tiles.Values)
        writer.Write(png);
    return ico.ToArray();
}
