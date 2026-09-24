namespace EternalfestDesktop.Domain;

/// <summary>A text in the contrée's main locale, with the translations its author provided.</summary>
public sealed record LocalizedText(string Default, IReadOnlyDictionary<string, string> Translations)
{
    public static LocalizedText Untranslated(string text) => new(text, new Dictionary<string, string>());

    public string In(string locale) => Translations.GetValueOrDefault(locale, Default);
}
