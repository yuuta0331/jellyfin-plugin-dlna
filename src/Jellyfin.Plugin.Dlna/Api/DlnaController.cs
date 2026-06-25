using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Jellyfin.Plugin.Dlna.Maintenance;
using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Dlna.Api;

/// <summary>
/// Dlna Controller.
/// </summary>
[ApiController]
[Route("Dlna")]
[Authorize(Policy = Policies.RequiresElevation)]
public class DlnaController : ControllerBase
{
    private readonly IDlnaManager _dlnaManager;
    private readonly IDlnaStorageMaintenanceService _storageMaintenanceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaController"/> class.
    /// </summary>
    /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
    /// <param name="storageMaintenanceService">Instance of the <see cref="IDlnaStorageMaintenanceService"/> interface.</param>
    public DlnaController(
        IDlnaManager dlnaManager,
        IDlnaStorageMaintenanceService storageMaintenanceService)
    {
        _dlnaManager = dlnaManager;
        _storageMaintenanceService = storageMaintenanceService;
    }

    /// <summary>
    /// Get profile infos.
    /// </summary>
    /// <response code="200">Device profile infos returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the device profile infos.</returns>
    [HttpGet("ProfileInfos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<DeviceProfileInfo>> GetProfileInfos()
    {
        return Ok(_dlnaManager.GetProfileInfos());
    }

    /// <summary>
    /// Gets the default profile.
    /// </summary>
    /// <response code="200">Default device profile returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the default profile.</returns>
    [HttpGet("Profiles/Default")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<DeviceProfile> GetDefaultProfile()
    {
        return _dlnaManager.GetDefaultProfile();
    }

    /// <summary>
    /// Gets a single profile.
    /// </summary>
    /// <param name="profileId">Profile Id.</param>
    /// <response code="200">Device profile returned.</response>
    /// <response code="404">Device profile not found.</response>
    /// <returns>An <see cref="OkResult"/> containing the profile on success, or a <see cref="NotFoundResult"/> if device profile not found.</returns>
    [HttpGet("Profiles/{profileId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DeviceProfile> GetProfile([FromRoute, Required] string profileId)
    {
        var profile = _dlnaManager.GetProfile(profileId);
        if (profile is null)
        {
            return NotFound();
        }

        return profile;
    }

    /// <summary>
    /// Deletes a profile.
    /// </summary>
    /// <param name="profileId">Profile id.</param>
    /// <response code="204">Device profile deleted.</response>
    /// <response code="404">Device profile not found.</response>
    /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if profile not found.</returns>
    [HttpDelete("Profiles/{profileId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult DeleteProfile([FromRoute, Required] string profileId)
    {
        var existingDeviceProfile = _dlnaManager.GetProfile(profileId);
        if (existingDeviceProfile is null)
        {
            return NotFound();
        }

        _dlnaManager.DeleteProfile(profileId);
        return NoContent();
    }

    /// <summary>
    /// Creates a profile.
    /// </summary>
    /// <param name="deviceProfile">Device profile.</param>
    /// <response code="204">Device profile created.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Profiles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult CreateProfile([FromBody] DlnaDeviceProfile deviceProfile)
    {
        _dlnaManager.CreateProfile(deviceProfile);
        return NoContent();
    }

    /// <summary>
    /// Updates a profile.
    /// </summary>
    /// <param name="profileId">Profile id.</param>
    /// <param name="deviceProfile">Device profile.</param>
    /// <response code="204">Device profile updated.</response>
    /// <response code="404">Device profile not found.</response>
    /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if profile not found.</returns>
    [HttpPost("Profiles/{profileId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult UpdateProfile([FromRoute, Required] string profileId, [FromBody] DlnaDeviceProfile deviceProfile)
    {
        var existingDeviceProfile = _dlnaManager.GetProfile(profileId);
        if (existingDeviceProfile is null)
        {
            return NotFound();
        }

        _dlnaManager.UpdateProfile(profileId, deviceProfile);
        return NoContent();
    }

    /// <summary>
    /// Gets DLNA storage and cache statistics.
    /// </summary>
    /// <response code="200">Storage statistics returned.</response>
    /// <returns>Storage statistics.</returns>
    [HttpGet("Storage/Stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<DlnaStorageStatsDto> GetStorageStats()
    {
        return Ok(_storageMaintenanceService.GetStats());
    }

    /// <summary>
    /// Clears the browse response cache.
    /// </summary>
    /// <response code="204">Browse cache cleared.</response>
    [HttpPost("Storage/ClearBrowseCache")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult ClearBrowseCache()
    {
        _storageMaintenanceService.ClearBrowseCache();
        return NoContent();
    }

    /// <summary>
    /// Clears the child count cache.
    /// </summary>
    /// <response code="204">Child count cache cleared.</response>
    [HttpPost("Storage/ClearChildCountCache")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult ClearChildCountCache()
    {
        _storageMaintenanceService.ClearChildCountCache();
        return NoContent();
    }

    /// <summary>
    /// Clears the virtual index database.
    /// </summary>
    /// <response code="204">Index cleared.</response>
    [HttpPost("Storage/ClearIndex")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult ClearIndex()
    {
        _storageMaintenanceService.ClearIndex();
        return NoContent();
    }

    /// <summary>
    /// Clears all caches and the index without rebuilding.
    /// </summary>
    /// <response code="204">All storage cleared.</response>
    [HttpPost("Storage/ClearAll")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult ClearAllStorage()
    {
        _storageMaintenanceService.ClearAll();
        return NoContent();
    }

    /// <summary>
    /// Rebuilds the virtual index in the background.
    /// </summary>
    /// <param name="prewarm">Whether to prewarm browse responses after rebuilding.</param>
    /// <response code="202">Rebuild started.</response>
    /// <response code="409">Maintenance already running.</response>
    [HttpPost("Storage/RebuildIndex")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult RebuildIndex([FromQuery] bool prewarm = false)
    {
        if (!_storageMaintenanceService.RebuildIndexAsync(prewarm))
        {
            return Conflict();
        }

        return Accepted();
    }

    /// <summary>
    /// Clears all storage and rebuilds the virtual index in the background.
    /// </summary>
    /// <param name="prewarm">Whether to prewarm browse responses after rebuilding.</param>
    /// <response code="202">Clear and rebuild started.</response>
    /// <response code="409">Maintenance already running.</response>
    [HttpPost("Storage/ClearAndRebuild")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult ClearAndRebuild([FromQuery] bool prewarm = false)
    {
        if (!_storageMaintenanceService.ClearAndRebuildAsync(prewarm))
        {
            return Conflict();
        }

        return Accepted();
    }
}
