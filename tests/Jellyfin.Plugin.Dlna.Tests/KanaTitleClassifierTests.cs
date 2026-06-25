using Jellyfin.Plugin.Dlna.ContentDirectory;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class KanaTitleClassifierTests
{
    private static readonly KanaClassificationOptions DefaultOptions = KanaClassificationOptions.Default;

    [Theory]
    [InlineData("おしのこ", 0)]
    [InlineData("【おしのこ】", 0)]
    [InlineData("ヴァイオレット・エヴァーガーデン", 0)]
    [InlineData("ガンダム", 1)]
    [InlineData("ヱヴァンゲリヲン", 0)]
    [InlineData("Re:ゼロから始める異世界生活", 10)]
    [InlineData("86 -エイティシックス-", 10)]
    [InlineData("ぼっち・ざ・ろっく！", 5)]
    [InlineData("キルラキル", 1)]
    [InlineData("きめつのやいば", 1)]
    [InlineData("ONE PIECE", 10)]
    [InlineData("SPY×FAMILY", 10)]
    public void Classify_Examples_ReturnExpectedRow(string sortName, int expectedRow)
    {
        var row = KanaRowHelper.Classify(sortName, null, DefaultOptions);
        Assert.Equal(expectedRow, row);
    }

    [Fact]
    public void Classify_PrefixStripping_UsesReadingAfterPrefix()
    {
        var row = KanaRowHelper.Classify("劇場版 きめつのやいば", null, DefaultOptions);
        Assert.Equal(1, row);
    }

    [Fact]
    public void Classify_PrefixStripping_CanBeDisabled()
    {
        var options = new KanaClassificationOptions
        {
            EnablePrefixStripping = false,
            Prefixes = DefaultOptions.Prefixes
        };

        var row = KanaRowHelper.Classify("劇場版きめつのやいば", null, options);
        Assert.Equal(KanaRowHelper.OtherRowIndex, row);
    }

    [Fact]
    public void Classify_KanjiOnlySortName_ReturnsOtherRow()
    {
        var row = KanaRowHelper.Classify("鬼滅刃", null, DefaultOptions);
        Assert.Equal(KanaRowHelper.OtherRowIndex, row);
    }

    [Fact]
    public void Classify_EmptyValues_ReturnsOtherRow()
    {
        var row = KanaRowHelper.Classify(null, null, DefaultOptions);
        Assert.Equal(KanaRowHelper.OtherRowIndex, row);
    }

    [Fact]
    public void Classify_FallsBackToName_WhenSortNameMissing()
    {
        var row = KanaRowHelper.Classify(null, "あにめ", DefaultOptions);
        Assert.Equal(0, row);
    }

    [Theory]
    [InlineData("ぼっち・ざ・ろっく！", 5)]
    [InlineData("Re:ゼロから始める異世界生活", 10)]
    public void MatchesRow_ReturnsTrueOnlyForMatchingRow(string sortName, int rowIndex)
    {
        Assert.True(KanaRowHelper.MatchesRow(sortName, null, rowIndex, DefaultOptions));
        Assert.False(KanaRowHelper.MatchesRow(sortName, null, rowIndex + 1, DefaultOptions));
    }

    [Theory]
    [InlineData("Sousou no Frieren", "葬送のフリーレン", 11)]
    [InlineData("Otome Game Sekai wa Mob ni Kibishii Sekai desu", "乙女ゲー世界はモブに厳しい世界です", 11)]
    [InlineData("Isekai Maou to Shoukan Shoujo no Dorei Majutsu", "異世界魔王と召喚少女の奴隷魔術", 11)]
    [InlineData("Youkoso Jitsuryoku Shijou Shugi no Kyoushitsu e", "ようこそ実力至上主義の教室へ", 7)]
    [InlineData("Kimetsu no Yaiba", "きめつのやいば", 1)]
    [InlineData("ONE PIECE", "ONE PIECE", 10)]
    public void Classify_RomanizedSortNameWithJapaneseName_UsesFirstCharacterOnly(
        string sortName,
        string name,
        int expectedRow)
    {
        var row = KanaRowHelper.Classify(sortName, name, DefaultOptions);
        Assert.Equal(expectedRow, row);
    }

    [Fact]
    public void Classify_KanaSortNameTakesPriorityOverKanjiName()
    {
        var row = KanaRowHelper.Classify("きめつのやいば", "鬼滅の刃", DefaultOptions);
        Assert.Equal(1, row);
    }

    [Fact]
    public void Classify_DoesNotUseParticleAfterKanjiPrefix()
    {
        var row = KanaRowHelper.Classify(null, "異世界魔王と召喚少女の奴隷魔術", DefaultOptions);
        Assert.Equal(KanaRowHelper.OtherRowIndex, row);
    }
}
