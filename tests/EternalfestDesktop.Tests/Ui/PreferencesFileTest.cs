using EternalfestDesktop.Domain;
using EternalfestDesktop.Ui;

namespace EternalfestDesktop.Tests.Ui;

public sealed class PreferencesFileTest : IDisposable
{
    private static readonly Guid Contree = Guid.Parse("0dc0d559-de83-4e0c-982d-fc56100dfdd5");

    private readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("eternalfest-desktop-tests-");

    public void Dispose() => _folder.Delete(recursive: true);

    private string PreferencesPath => Path.Combine(_folder.FullName, "preferences.json");

    /// @spec profile::complete-by-default
    [Theory]
    [InlineData("")]
    [InlineData(""", "Profile": "Legendary" """)]
    [InlineData(""", "Profile": 42 """)]
    [InlineData(""", "Profile": null """)]
    public void Reads_a_missing_or_unknown_profile_as_the_complete_profile(string profile)
    {
        File.WriteAllText(PreferencesPath, $$"""
            { "UiLanguage": "fr", "Choices": { "{{Contree}}": { "Mode": "multicoop", "Volume": 40{{profile}} } } }
            """);

        var preferences = new PreferencesFile(PreferencesPath).Load();

        Assert.Equal("fr", preferences.UiLanguage);
        var choices = preferences.Choices[Contree];
        Assert.Equal(PlayerProfile.Complete, choices.Profile);
        Assert.Equal("multicoop", choices.Mode);
        Assert.Equal(40, choices.Volume);
    }

    /// @spec ui::profile-picker
    [Fact]
    public void Remembers_the_profile_by_its_name()
    {
        var file = new PreferencesFile(PreferencesPath);
        file.Current.Choices[Contree] = new RunChoices(Profile: PlayerProfile.NewPlayer);

        file.Save();

        Assert.Contains("\"NewPlayer\"", File.ReadAllText(PreferencesPath), StringComparison.Ordinal);
        Assert.Equal(PlayerProfile.NewPlayer, new PreferencesFile(PreferencesPath).Load().Choices[Contree].Profile);
    }
}
