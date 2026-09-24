namespace EternalfestDesktop.Domain;

/// <summary>A public contrée as listed in the catalog.</summary>
public sealed record CatalogEntry(
    GameId Id,
    string? Key,
    string Version,
    LocalizedText DisplayName,
    LocalizedText Description,
    Blob? Icon);
