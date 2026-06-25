using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="BrowsePagingResolver"/>.
/// </summary>
public class BrowsePagingResolverTests
{
  [Fact]
  public void Resolve_QuestMode_ReturnsUnlimited()
  {
    var config = new DlnaPluginConfiguration
    {
      EnableQuestCompatibilityMode = true,
      RespectRequestedCount = true,
      MaxBrowseItemsPerResponse = 50
    };

    var result = BrowsePagingResolver.Resolve(config, requestedCount: 10, startIndex: 0);

    Assert.Null(result.Limit);
    Assert.False(result.StrictTotalMatches);
  }

  [Fact]
  public void Resolve_RespectDisabled_ReturnsUnlimited()
  {
    var config = new DlnaPluginConfiguration
    {
      EnableQuestCompatibilityMode = false,
      RespectRequestedCount = false,
      MaxBrowseItemsPerResponse = 1000
    };

    var result = BrowsePagingResolver.Resolve(config, requestedCount: 10, startIndex: 0);

    Assert.Null(result.Limit);
  }

  [Fact]
  public void Resolve_RespectEnabled_CapsToRequestedCount()
  {
    var config = new DlnaPluginConfiguration
    {
      EnableQuestCompatibilityMode = false,
      RespectRequestedCount = true,
      EnableStrictTotalMatches = true,
      MaxBrowseItemsPerResponse = 1000
    };

    var result = BrowsePagingResolver.Resolve(config, requestedCount: 10, startIndex: 5);

    Assert.Equal(10, result.Limit);
    Assert.Equal(5, result.StartIndex);
    Assert.True(result.StrictTotalMatches);
  }
}
