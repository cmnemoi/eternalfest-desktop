using System.Security.Cryptography;
using System.Text.Json.Nodes;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Tests.Support;

/// <summary>A contrée as Eternalfest publishes it: its game document and the bytes of every blob it references.</summary>
internal sealed class PublishedContree
{
    private readonly List<(string Role, BlobId Id, byte[] Bytes)> _files = [];
    private bool _onBaseEngine;

    private PublishedContree(string name) => Name = name;

    public string Name { get; }
    public GameId Id { get; } = new(Guid.NewGuid());
    public string Version { get; private set; } = "1.0.0";
    public string LoaderVersion { get; private set; } = "5.1.2";
    public string Families { get; private set; } = "0,1,2,1000";

    public JsonObject Modes { get; } = new()
    {
        ["solo"] = Mode("Aventure", ("mirror", "Miroir", true, true), ("ninja", "Ninjutsu", true, false), ("debug", "Debug", false, false)),
        ["multicoop"] = Mode("Multi Coopératif", ("lifesharing", "Partage de vies", true, false)),
    };

    public IReadOnlyDictionary<BlobId, byte[]> Blobs => _files.ToDictionary(file => file.Id, file => file.Bytes);
    public long ByteSize => _files.Sum(file => (long)file.Bytes.Length);

    /// <summary>A contrée with an engine, a patcher, content, its translation, two musics and an icon.</summary>
    public static PublishedContree Named(string name) =>
        new PublishedContree(name)
            .With("engine", Bytes(64)).With("patcher", Bytes(16)).With("content", Bytes(32))
            .With("content_i18n", Bytes(8)).WithMusic(Bytes(24)).WithMusic(Bytes(24)).With("icon", Bytes(4));

    public PublishedContree InVersion(string version)
    {
        Version = version;
        return this;
    }

    public PublishedContree RequiringLoader(string version)
    {
        LoaderVersion = version;
        return this;
    }

    public PublishedContree OnBaseEngine()
    {
        _onBaseEngine = true;
        _files.RemoveAll(file => file.Role == "engine");
        return this;
    }

    public PublishedContree WithFamilies(string families)
    {
        Families = families;
        return this;
    }

    public PublishedContree WithMusic(byte[] bytes) => WithMusic(new BlobId(Guid.NewGuid()), bytes);

    /// <summary>Adds a music that may also be published by another contrée, under the same blob id.</summary>
    public PublishedContree WithMusic(BlobId id, byte[] bytes)
    {
        _files.Add(("music", id, bytes));
        return this;
    }

    /// <summary>Publishes a digest that doesn't match the blob's bytes.</summary>
    public PublishedContree WithCorruptedContent()
    {
        var content = _files.Single(file => file.Role == "content");
        _corrupted = content.Id;
        return this;
    }

    private BlobId? _corrupted;

    private PublishedContree With(string role, byte[] bytes)
    {
        _files.RemoveAll(file => file.Role == role);
        _files.Add((role, new BlobId(Guid.NewGuid()), bytes));
        return this;
    }

    public BlobId BlobOf(string role) => _files.First(file => file.Role == role).Id;

    public string Document()
    {
        var build = new JsonObject
        {
            ["version"] = Version,
            ["created_at"] = "2026-01-01T00:00:00.000Z",
            ["git_commit_ref"] = null,
            ["main_locale"] = "fr-FR",
            ["display_name"] = Name,
            ["description"] = $"Description of {Name}",
            ["loader"] = LoaderVersion,
            ["engine"] = new JsonObject { ["type"] = "V96" },
            ["debug"] = null,
            ["musics"] = new JsonArray(),
            ["modes"] = Modes.DeepClone(),
            ["families"] = Families,
            ["category"] = "Other",
            ["i18n"] = new JsonObject(),
        };
        foreach (var (role, id, bytes) in _files)
        {
            var blob = BlobNode(id, bytes);
            switch (role)
            {
                case "engine" when !_onBaseEngine:
                    build["engine"] = new JsonObject { ["type"] = "Custom", ["blob"] = blob };
                    break;
                case "patcher":
                    build["patcher"] = new JsonObject
                    {
                        ["blob"] = blob,
                        ["framework"] = new JsonObject { ["name"] = "patchman", ["version"] = "0.10.14" },
                        ["meta"] = null,
                    };
                    break;
                case "music":
                    build["musics"]!.AsArray().Add(new JsonObject { ["blob"] = blob, ["display_name"] = "./musics/music.mp3" });
                    break;
                default:
                    build[role] = blob;
                    break;
            }
        }

        return new JsonObject
        {
            ["type"] = "Game",
            ["id"] = Id.ToString(),
            ["created_at"] = "2026-01-01T00:00:00.000Z",
            ["key"] = Name.ToLowerInvariant().Replace(' ', '-'),
            ["owner"] = new JsonObject { ["type"] = "User", ["id"] = Guid.Empty.ToString(), ["display_name"] = "Author" },
            ["channels"] = new JsonObject
            {
                ["offset"] = 0,
                ["limit"] = 1,
                ["count"] = 1,
                ["is_count_exact"] = false,
                ["active"] = new JsonObject
                {
                    ["type"] = "GameChannel",
                    ["key"] = "main",
                    ["is_enabled"] = true,
                    ["is_pinned"] = false,
                    ["publication_date"] = null,
                    ["sort_update_date"] = "2026-01-01T00:00:00.000Z",
                    ["default_permission"] = "Play",
                    ["build"] = build,
                },
            },
        }.ToJsonString();
    }

    /// <summary>The contrée as the catalog lists it.</summary>
    public JsonObject ListingItem()
    {
        var game = JsonNode.Parse(Document())!.AsObject();
        var channel = game["channels"]!["active"]!.AsObject();
        var build = channel["build"]!.AsObject();
        return new JsonObject
        {
            ["type"] = "Game",
            ["id"] = game["id"]!.DeepClone(),
            ["created_at"] = game["created_at"]!.DeepClone(),
            ["key"] = game["key"]!.DeepClone(),
            ["owner"] = game["owner"]!.DeepClone(),
            ["channels"] = new JsonObject
            {
                ["offset"] = 0,
                ["limit"] = 1,
                ["count"] = 1,
                ["is_count_exact"] = false,
                ["items"] = new JsonArray(new JsonObject
                {
                    ["type"] = "GameChannel",
                    ["key"] = channel["key"]!.DeepClone(),
                    ["is_enabled"] = true,
                    ["is_pinned"] = false,
                    ["publication_date"] = null,
                    ["sort_update_date"] = "2026-01-01T00:00:00.000Z",
                    ["default_permission"] = "Play",
                    ["build"] = new JsonObject
                    {
                        ["version"] = build["version"]!.DeepClone(),
                        ["git_commit_ref"] = null,
                        ["main_locale"] = build["main_locale"]!.DeepClone(),
                        ["display_name"] = build["display_name"]!.DeepClone(),
                        ["description"] = build["description"]!.DeepClone(),
                        ["icon"] = build["icon"]?.DeepClone(),
                        ["i18n"] = new JsonObject(),
                    },
                }),
            },
        };
    }

    private JsonObject BlobNode(BlobId id, byte[] bytes) => new()
    {
        ["type"] = "Blob",
        ["id"] = id.ToString(),
        ["media_type"] = "application/octet-stream",
        ["byte_size"] = bytes.Length,
        ["digest"] = new JsonObject
        {
            ["sha2_256"] = id == _corrupted ? new string('0', 64) : Convert.ToHexStringLower(SHA256.HashData(bytes)),
            ["sha3_256"] = "",
        },
    };

    public static byte[] Bytes(int length) => RandomNumberGenerator.GetBytes(length);

    private static JsonObject Mode(string displayName, params (string Key, string DisplayName, bool IsVisible, bool IsEnabled)[] options)
    {
        var mode = new JsonObject { ["display_name"] = displayName, ["is_visible"] = true, ["options"] = new JsonObject() };
        foreach (var option in options)
            mode["options"]![option.Key] = new JsonObject
            {
                ["display_name"] = option.DisplayName,
                ["is_visible"] = option.IsVisible,
                ["is_enabled"] = option.IsEnabled,
                ["default_value"] = false,
            };
        return mode;
    }
}
