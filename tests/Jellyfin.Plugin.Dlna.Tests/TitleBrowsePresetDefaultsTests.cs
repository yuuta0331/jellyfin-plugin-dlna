using Jellyfin.Plugin.Dlna.Configuration;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class TitleBrowsePresetDefaultsTests
{
    [Fact]
    public void AlphabetPreset_ContainsTwentyEightGroups()
    {
        var preset = TitleBrowsePresetDefaults.CreateAlphabetPreset();

        Assert.Equal(TitleBrowsePresetDefaults.AlphabetPresetId, preset.Id);
        Assert.Equal(28, preset.Groups.Length);
        Assert.Equal("a", preset.Groups[0].Id);
        Assert.Equal(TitleBrowsePresetDefaults.DigitsGroupId, preset.Groups[26].Id);
        Assert.Equal(TitleBrowsePresetDefaults.OtherGroupId, preset.Groups[27].Id);
    }

    [Fact]
    public void JapaneseKanaPreset_ContainsTwelveGroups()
    {
        var preset = TitleBrowsePresetDefaults.CreateJapaneseKanaPreset();

        Assert.Equal(TitleBrowsePresetDefaults.JapaneseKanaPresetId, preset.Id);
        Assert.Equal(12, preset.Groups.Length);
        Assert.Equal("row-0", preset.Groups[0].Id);
        Assert.Equal("row-11", preset.Groups[11].Id);
        Assert.Equal(TitleBrowseMatchMode.Other, preset.Groups[11].MatchMode);
    }
}
