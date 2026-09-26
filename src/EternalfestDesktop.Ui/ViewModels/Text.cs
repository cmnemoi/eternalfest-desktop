using System.Globalization;
using System.Text;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Ui.Resources;

namespace EternalfestDesktop.Ui.ViewModels;

internal static class Text
{
    public static string Format(string format, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, format, arguments);

    /// <summary>Lower case, without accents: "Élite" and "elite" match.</summary>
    public static string Searchable(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>The contrée's text in the UI language when its author translated it.</summary>
    public static string Localized(LocalizedText text)
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var translation = text.Translations.FirstOrDefault(pair => pair.Key.StartsWith(language + "-", StringComparison.OrdinalIgnoreCase));
        return translation.Value ?? text.Default;
    }

    /// <summary>What went wrong, in words a player understands.</summary>
    public static string ErrorMessage(Exception exception) => exception switch
    {
        EternalfestUnreachableException => Strings.ErrorUnreachable,
        GameNotFoundException => Strings.ErrorNotFound,
        CorruptedBlobException => Strings.ErrorCorrupted,
        FlashPlayerMissingException => Strings.ErrorFlashPlayerMissing,
        GameAlreadyRunningException => Strings.ErrorAlreadyPlaying,
        _ => Format(Strings.ErrorUnexpected, exception.Message),
    };
}
