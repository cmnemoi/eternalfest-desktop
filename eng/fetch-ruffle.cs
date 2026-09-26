// Downloads the pinned Ruffle desktop (eng/ruffle.json) for a runtime identifier into artifacts/ruffle/{rid},
// checking its SHA-256. Usage: mise run fetch-ruffle [rid]   (defaults to this machine's)
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

var root = Path.GetFullPath(Path.Combine(AppContext.GetData("EntryPointFileDirectoryPath") as string ?? "eng", ".."));
var rid = args.FirstOrDefault() ?? RuntimeInformation.RuntimeIdentifier;
using var pins = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "eng", "ruffle.json")));
var version = pins.RootElement.GetProperty("version").GetString()!;
if (!pins.RootElement.GetProperty("archives").TryGetProperty(rid, out var archive))
{
    Console.Error.WriteLine($"No Ruffle {version} archive is pinned for {rid}.");
    return 1;
}

var destination = Path.Combine(root, "artifacts", "ruffle", rid);
var marker = Path.Combine(destination, ".version");
if (File.Exists(marker) && File.ReadAllText(marker) == version)
{
    Console.WriteLine($"Ruffle {version} for {rid} is already in {destination}.");
    return 0;
}

var url = archive.GetProperty("url").GetString()!;
Console.WriteLine($"Downloading Ruffle {version} for {rid} from {url}");
using var http = new HttpClient();
var bytes = await http.GetByteArrayAsync(url);
var digest = Convert.ToHexStringLower(SHA256.HashData(bytes));
var expected = archive.GetProperty("sha256").GetString()!;
if (digest != expected)
{
    Console.Error.WriteLine($"Ruffle archive SHA-256 is {digest}, expected {expected}.");
    return 1;
}

if (Directory.Exists(destination))
    Directory.Delete(destination, recursive: true);
Directory.CreateDirectory(destination);
using var stream = new MemoryStream(bytes);
if (url.EndsWith(".zip", StringComparison.Ordinal))
{
    ZipFile.ExtractToDirectory(stream, destination);
}
else
{
    using var gzip = new GZipStream(stream, CompressionMode.Decompress);
    await TarFile.ExtractToDirectoryAsync(gzip, destination, overwriteFiles: true);
}
File.WriteAllText(marker, version);
Console.WriteLine($"Ruffle {version} for {rid} is in {destination}.");
return 0;
