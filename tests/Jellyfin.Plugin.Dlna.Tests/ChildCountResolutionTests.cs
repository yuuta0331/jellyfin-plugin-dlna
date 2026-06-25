using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

/// <summary>
/// Tests for <see cref="ChildCountResolution"/>.
/// </summary>
public class ChildCountResolutionTests
{
  [Fact]
  public void ResolveWithoutQuery_QuestMode_ReturnsNull()
  {
    var config = new DlnaPluginConfiguration
    {
      EnableQuestCompatibilityMode = true,
      ChildCountCalculation = ChildCountMode.Accurate
    };

    var result = ChildCountResolution.ResolveWithoutQuery(config, isStubFolder: false, cachedCount: 42);

    Assert.Null(result);
  }

  [Fact]
  public void ResolveWithoutQuery_StubEstimate_ReturnsZero()
  {
    var config = new DlnaPluginConfiguration
    {
      ChildCountCalculation = ChildCountMode.Estimate
    };

    var result = ChildCountResolution.ResolveWithoutQuery(config, isStubFolder: true, cachedCount: null);

    Assert.Equal(0, result);
  }

  [Fact]
  public void ResolveWithoutQuery_Disabled_ReturnsNull()
  {
    var config = new DlnaPluginConfiguration
    {
      ChildCountCalculation = ChildCountMode.Disabled
    };

    var result = ChildCountResolution.ResolveWithoutQuery(config, isStubFolder: false, cachedCount: 99);

    Assert.Null(result);
  }

  [Fact]
  public void RequiresAccurateQuery_AccuratePhysicalFolder_ReturnsTrue()
  {
    var config = new DlnaPluginConfiguration
    {
      ChildCountCalculation = ChildCountMode.Accurate
    };

    Assert.True(ChildCountResolution.RequiresAccurateQuery(config, isStubFolder: false));
    Assert.False(ChildCountResolution.RequiresAccurateQuery(config, isStubFolder: true));
  }
}
