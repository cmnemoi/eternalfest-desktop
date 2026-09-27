// Downloads the pinned Flash projector (eng/flash-player.json) for a runtime identifier into artifacts/flash-player/{rid},
// checking each SHA-256, with its notices (assets/flash-player). On Linux, it also bundles GTK 2 and NSS from Debian,
// and builds the library shaping the projector's window (native/projector-window): that needs tar, xz and a C compiler (cc).
// On macOS, it extracts the projector's app bundle from Adobe's disk image, and builds the same library for it: that needs
// hdiutil, ditto, clang and codesign, so the Mac projector is fetched on a Mac only.
// Usage: mise run fetch-flash-player [rid]   (defaults to this machine's)
using System.Diagnostics;
using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var root = Path.GetFullPath(Path.Combine(AppContext.GetData("EntryPointFileDirectoryPath") as string ?? "eng", ".."));
var rid = args.FirstOrDefault() ?? RuntimeInformation.RuntimeIdentifier;
using var pins = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "eng", "flash-player.json")));
var version = pins.RootElement.GetProperty("version").GetString()!;
if (!pins.RootElement.GetProperty("runtimes").TryGetProperty(rid, out var pinned))
{
    // Not an error: the app plays in Ruffle on the systems it ships no projector for (ADR 0008)
    Console.WriteLine($"No Flash projector is pinned for {rid}: contrées will play in Ruffle.");
    return 0;
}

var destination = Path.Combine(root, "artifacts", "flash-player", rid);
var libraries = Path.Combine(destination, "lib");
var marker = Path.Combine(destination, ".version");
var pinsDigest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(pinned.GetRawText())));
if (File.Exists(marker) && File.ReadAllText(marker) == pinsDigest)
{
    Console.WriteLine($"Flash projector {version} for {rid} is already in {destination}.");
}
else
{
    if (Directory.Exists(destination))
        Directory.Delete(destination, recursive: true);
    Directory.CreateDirectory(destination);
    using var http = new HttpClient();

    var projector = pinned.GetProperty("projector");
    var projectorUrl = projector.GetProperty("url").GetString()!;
    Console.WriteLine($"Downloading the Flash projector {version} for {rid} from {projectorUrl}");
    var projectorBytes = await Download(http, projector);
    if (projectorUrl.EndsWith(".exe", StringComparison.Ordinal))
    {
        await File.WriteAllBytesAsync(Path.Combine(destination, "flashplayer.exe"), projectorBytes);
    }
    else if (projectorUrl.EndsWith(".dmg", StringComparison.Ordinal))
    {
        CopyAppFromDiskImage(projectorBytes, "Flash Player.app", destination);
    }
    else
    {
        using var gzip = new GZipStream(new MemoryStream(projectorBytes), CompressionMode.Decompress);
        await TarFile.ExtractToDirectoryAsync(gzip, destination, overwriteFiles: true);
    }

    var notices = Path.Combine(root, "assets", "flash-player");
    File.Copy(Path.Combine(notices, "NOTICE.md"), Path.Combine(destination, "NOTICE.md"));
    if (pinned.TryGetProperty("libraries", out var pinnedLibraries))
    {
        var licenses = Path.Combine(destination, "licenses");
        Directory.CreateDirectory(libraries);
        Directory.CreateDirectory(licenses);
        foreach (var license in Directory.EnumerateFiles(Path.Combine(notices, "licenses")))
            File.Copy(license, Path.Combine(licenses, Path.GetFileName(license)));
        foreach (var library in pinnedLibraries.EnumerateArray())
        {
            var package = library.GetProperty("package").GetString()!;
            Console.WriteLine($"Bundling {package}");
            BundleSharedLibraries(DebianData(await Download(http, library)), package.Split(' ')[0], libraries, licenses);
        }
    }
    File.WriteAllText(marker, pinsDigest);
}

if (rid.StartsWith("linux-", StringComparison.Ordinal))
{
    var source = Path.Combine(root, "native", "projector-window", "projector-window.c");
    Run("cc", "-shared", "-fPIC", "-O2", "-Wall", "-Wextra", "-Werror", "-o", Path.Combine(libraries, "libprojector-window.so"), source, "-ldl");
}
if (rid.StartsWith("osx-", StringComparison.Ordinal))
{
    // The projector is Intel only: its library too, which Rosetta 2 runs with it
    var source = Path.Combine(root, "native", "projector-window", "projector-window.m");
    var library = Path.Combine(destination, "libprojector-window.dylib");
    Run("clang", "-arch", "x86_64", "-dynamiclib", "-fobjc-arc", "-mmacosx-version-min=11.0", "-O2", "-Wall", "-Wextra", "-Werror", "-framework", "AppKit", "-o", library, source);
    Run("codesign", "--force", "--sign", "-", library);
}
Console.WriteLine($"Flash projector {version} for {rid} is in {destination}.");
return 0;

static async Task<byte[]> Download(HttpClient http, JsonElement pin)
{
    var url = pin.GetProperty("url").GetString()!;
    var bytes = await http.GetByteArrayAsync(url);
    var digest = Convert.ToHexStringLower(SHA256.HashData(bytes));
    var expected = pin.GetProperty("sha256").GetString()!;
    if (digest != expected)
        throw new InvalidOperationException($"{url} has SHA-256 {digest}, expected {expected}.");
    return bytes;
}

// A .deb is an ar archive; its files are in data.tar.xz, which .NET can't read: tar and xz extract it
static string DebianData(byte[] package)
{
    const int GlobalHeaderLength = 8;
    const int EntryHeaderLength = 60;
    var position = GlobalHeaderLength;
    while (position < package.Length)
    {
        var name = Encoding.ASCII.GetString(package, position, 16).Trim().TrimEnd('/');
        var length = int.Parse(Encoding.ASCII.GetString(package, position + 48, 10).Trim(), CultureInfo.InvariantCulture);
        if (name == "data.tar.xz")
        {
            var extracted = Directory.CreateTempSubdirectory("eternalfest-desktop-deb-").FullName;
            var archive = Path.Combine(extracted, name);
            File.WriteAllBytes(archive, package.AsSpan(position + EntryHeaderLength, length).ToArray());
            Run("tar", "-xJf", archive, "-C", extracted);
            return extracted;
        }
        position += EntryHeaderLength + length + (length % 2);
    }
    throw new InvalidOperationException("The Debian package has no data.tar.xz.");
}

// Each library under the name the projector links it by (a link's name with its target's content), and its copyright
static void BundleSharedLibraries(string debianData, string package, string libraries, string licenses)
{
    File.Copy(Path.Combine(debianData, "usr", "share", "doc", package, "copyright"), Path.Combine(licenses, $"{package}.copyright"));
    var folder = new DirectoryInfo(Path.Combine(debianData, "usr", "lib", "x86_64-linux-gnu"));
    var sharedLibraries = folder.EnumerateFiles("*.so*").ToList();
    var linkTargets = sharedLibraries.Where(library => library.LinkTarget is not null).Select(link => link.ResolveLinkTarget(returnFinalTarget: true)!.Name).ToHashSet();
    foreach (var library in sharedLibraries.Where(library => !linkTargets.Contains(library.Name)))
        File.Copy(library.LinkTarget is null ? library.FullName : library.ResolveLinkTarget(returnFinalTarget: true)!.FullName, Path.Combine(libraries, library.Name), overwrite: true);
    Directory.Delete(debianData, recursive: true);
}

// ditto keeps the app bundle as Adobe signed it
static void CopyAppFromDiskImage(byte[] diskImage, string app, string destination)
{
    var mounted = Directory.CreateTempSubdirectory("eternalfest-desktop-dmg-").FullName;
    var image = Path.Combine(mounted, "image.dmg");
    var mountPoint = Path.Combine(mounted, "volume");
    File.WriteAllBytes(image, diskImage);
    Run("hdiutil", "attach", "-nobrowse", "-readonly", "-quiet", "-mountpoint", mountPoint, image);
    try
    {
        Run("ditto", Path.Combine(mountPoint, app), Path.Combine(destination, app));
    }
    finally
    {
        Run("hdiutil", "detach", "-quiet", mountPoint);
        Directory.Delete(mounted, recursive: true);
    }
}

static void Run(string command, params string[] arguments)
{
    using var process = Process.Start(new ProcessStartInfo(command, arguments)) ?? throw new InvalidOperationException($"{command} didn't start.");
    process.WaitForExit();
    if (process.ExitCode != 0)
        throw new InvalidOperationException($"{command} {string.Join(' ', arguments)} exited with code {process.ExitCode}.");
}
