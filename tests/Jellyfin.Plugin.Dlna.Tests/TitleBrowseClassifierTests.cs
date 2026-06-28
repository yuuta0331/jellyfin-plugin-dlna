using System;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class TitleBrowseClassifierTests
{
    private static TitleBrowseOptions JapaneseKanaOptions()
        => TitleBrowseOptions.FromConfiguration(new DlnaPluginConfiguration
        {
            ActiveTitleBrowsePresetId = TitleBrowsePresetDefaults.JapaneseKanaPresetId
        }, Guid.Empty);

    private static TitleBrowseOptions AlphabetOptions()
        => TitleBrowseOptions.FromConfiguration(new DlnaPluginConfiguration(), Guid.Empty);

    [Theory]
    [InlineData("おしのこ", "row-0")]
    [InlineData("【おしのこ】", "row-0")]
    [InlineData("ガンダム", "row-1")]
    [InlineData("Re:ゼロから始める異世界生活", "row-10")]
    [InlineData("ONE PIECE", "row-10")]
    [InlineData("Apple Seed", "a")]
    [InlineData("banana", "b")]
    [InlineData("007", "0-9")]
    public void Classify_ReturnsExpectedGroup(string sortName, string expectedGroupId)
    {
        var options = expectedGroupId.StartsWith("row-", StringComparison.Ordinal)
            ? JapaneseKanaOptions()
            : AlphabetOptions();

        var groupId = TitleBrowseClassifier.Classify(sortName, null, options);
        Assert.Equal(expectedGroupId, groupId);
    }

    [Fact]
    public void Classify_LibraryRegexStrip_UsesReadingAfterPrefix()
    {
        var config = new DlnaPluginConfiguration
        {
            ActiveTitleBrowsePresetId = TitleBrowsePresetDefaults.JapaneseKanaPresetId,
            LibraryTitleBrowseOverrides =
            [
                new LibraryTitleBrowseOverride
                {
                    LibraryId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    TitleStripRegexes = ["^劇場版\\s*"]
                }
            ]
        };

        var options = TitleBrowseOptions.FromConfiguration(
            config,
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var groupId = TitleBrowseClassifier.Classify("劇場版 きめつのやいば", null, options);
        Assert.Equal("row-1", groupId);
    }

    [Fact]
    public void Classify_KanjiOnlySortName_ReturnsOtherGroup()
    {
        var groupId = TitleBrowseClassifier.Classify("鬼滅刃", null, JapaneseKanaOptions());
        Assert.Equal("row-11", groupId);
    }

    [Theory]
    [InlineData("Sousou no Frieren", "葬送のフリーレン", "row-11")]
    [InlineData("Kimetsu no Yaiba", "きめつのやいば", "row-1")]
    public void Classify_RomanizedSortNameWithJapaneseName_UsesFirstCharacterOnly(
        string sortName,
        string name,
        string expectedGroupId)
    {
        var groupId = TitleBrowseClassifier.Classify(sortName, name, JapaneseKanaOptions());
        Assert.Equal(expectedGroupId, groupId);
    }

    [Fact]
    public void Classify_UnmatchedAlphabetTitle_ReturnsOtherGroup()
    {
        var groupId = TitleBrowseClassifier.Classify("【作品】", null, AlphabetOptions());
        Assert.Equal(TitleBrowsePresetDefaults.OtherGroupId, groupId);
    }
}
