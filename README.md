<h1 align="center">Jellyfin DLNA Plugin (Fork)</h1>
<h3 align="center">Enhanced fork of the <a href="https://github.com/jellyfin/jellyfin-plugin-dlna">official Jellyfin DLNA plugin</a></h3>

<p align="center">
<img alt="Plugin Banner" src="https://raw.githubusercontent.com/jellyfin/jellyfin-ux/master/plugins/SVG/jellyfin-plugin-dlna.svg?sanitize=true"/>
<br/>
<br/>
<a href="https://github.com/yuuta0331/jellyfin-plugin-dlna/actions/workflows/test.yaml">
<img alt="GitHub Workflow Status" src="https://img.shields.io/github/actions/workflow/status/yuuta0331/jellyfin-plugin-dlna/test.yaml?branch=master">
</a>
<a href="https://github.com/yuuta0331/jellyfin-plugin-dlna">
<img alt="GPLv3 License" src="https://img.shields.io/github/license/yuuta0331/jellyfin-plugin-dlna.svg"/>
</a>
<a href="https://github.com/yuuta0331/jellyfin-plugin-dlna/releases">
<img alt="Current Release" src="https://img.shields.io/github/release/yuuta0331/jellyfin-plugin-dlna.svg"/>
</a>
</p>

## Install from this fork

Add this plugin repository in **Jellyfin Dashboard → Plugins → Repositories**:

```
https://yuuta0331.github.io/jellyfin-plugin-dlna/manifest.json
```

Then install or update **DLNA** from the plugin catalog.

> **Note:** This fork uses the same plugin GUID as the official DLNA plugin. Remove the official plugin repository from Jellyfin if you want to avoid catalog conflicts, and use this manifest only.

Official upstream: [jellyfin/jellyfin-plugin-dlna](https://github.com/jellyfin/jellyfin-plugin-dlna)

## Changes compared to the official plugin

This fork targets **Jellyfin 10.11.x** and focuses on DLNA Browse performance, device compatibility (especially Meta Quest), and usability improvements.

### Compatibility

- Pinned Jellyfin API packages to **10.11.10** for stable loading on 10.11.x servers
- `targetAbi` set to **10.11.0.0** for broader 10.11.x support

### DLNA Browse performance

- SQLite-backed virtual folder indexes for faster initial browse
- Multi-layer browse response caches and XML prewarm
- Reduced N+1 `childCount` queries during folder listing

### Browse features

- Virtual browse folders: mixed libraries, home videos, music videos
- Genre, year, recent releases, and hierarchy browse lists
- **Browse By Kana** (Japanese syllabary grouping)
- Paging settings for large result sets
- Poster/thumbnail display in virtual folder lists

### Device support

- **Meta Quest** profile and Quest compatibility mode
- Fix for Meta Quest 3 playback when indexed browse omits stream URLs
- Support for 3D, 4K, 8K, and VR video tags

### Settings and UI

- Tabbed settings layout with clearer help text
- Episode list image source option
- Startup index UI improvements
- Storage maintenance tools and gated debug logging

### Localization

- Japanese UI strings for plugin settings

## Requirements

- Jellyfin **10.11.0** or newer
- .NET **9.0** (server runtime; no separate install needed for end users)

## License

GPL-3.0 — same as the [official plugin](https://github.com/jellyfin/jellyfin-plugin-dlna).
