<h1 align="center">Jellyfin DLNA Plugin</h1>
<h3 align="center">Fork of the <a href="https://jellyfin.org">Jellyfin Project</a> DLNA plugin</h3>

<p align="center">
<img alt="Plugin Banner" src="https://raw.githubusercontent.com/jellyfin/jellyfin-ux/master/plugins/SVG/jellyfin-plugin-dlna.svg?sanitize=true"/>
<br/>
<br/>
<a href="https://github.com/yuuta0331/jellyfin-plugin-dlna/actions?query=workflow%3A%22Test+Plugin%22">
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

Add this plugin repository in Jellyfin Dashboard → Plugins → Repositories:

```
https://yuuta0331.github.io/jellyfin-plugin-dlna/manifest.json
```

Then install or update **DLNA** from the plugin catalog.

Upstream source: [jellyfin/jellyfin-plugin-dlna](https://github.com/jellyfin/jellyfin-plugin-dlna)

## Maintainer setup (one-time)

In the fork repository settings on GitHub:

1. **Actions → General → Workflow permissions**: enable **Read and write permissions**.
2. **Pages**: set source to branch `gh-pages` / root (created automatically on the first published release).

## Release via CI/CD

Releases are created by GitHub Actions. Do not create releases manually from the CLI.

### Quick release (recommended)

1. Update `version` and `changelog` in [`build.yaml`](build.yaml).
2. Commit and push to `master`.
3. Open **Actions** → **📦 Release Plugin** → **Run workflow** → **Run workflow**.

The workflow will:

- Build the plugin with JPRM
- Create GitHub Release `v<version>` with notes from `build.yaml`
- Upload ZIP and checksum files
- Update `gh-pages` / `manifest.json` for Jellyfin plugin catalog

### Optional: Release Drafter flow

On push to `master`, Release Drafter can prepare a draft release and open a PR to bump `build.yaml`. After merging the PR, publish the draft release on GitHub. The **📦 Release Plugin** workflow then uploads assets and updates the manifest.
