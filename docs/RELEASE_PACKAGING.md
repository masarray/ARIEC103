# ArIEC103 Release Packaging

This document describes the recommended release package flow for GitHub.

## Preferred release assets

For each public release, provide:

```text
ArIEC103-vX.Y.Z-win-x64-portable.zip
SHA256SUMS.txt
Source code zip/tar.gz from GitHub
```

The portable package is intended for engineers who want to run the tool without opening Visual Studio.

## Local package command

From repository root:

```powershell
pwsh ./scripts/publish-windows-portable.ps1 -Version 1.2.31
```

Expected output:

```text
artifacts/release/ArIEC103-v1.2.31-win-x64-portable.zip
artifacts/release/SHA256SUMS.txt
```

Verify package structure:

```powershell
pwsh ./scripts/verify-release-package.ps1 -PackagePath artifacts/release/ArIEC103-v1.2.31-win-x64-portable.zip
```

## GitHub Actions package flow

The workflow below creates the same portable ZIP and checksum:

```text
.github/workflows/release-package.yml
```

Manual release run:

1. Open **Actions**.
2. Select **Build Windows portable package**.
3. Click **Run workflow**.
4. Set `version`, for example `1.2.31`.
5. Keep **Create or update GitHub Release** enabled.
6. Leave `release_notes_file` empty unless you want to force a specific notes file.
7. Keep **Pre-release** enabled until the relay validation matrix is strong enough for a stable release.
8. Click **Run workflow**.

The workflow will build the portable ZIP, verify the package structure, upload a workflow artifact, create tag `vX.Y.Z` when needed, and publish these assets to GitHub Releases:

```text
ArIEC103-vX.Y.Z-win-x64-portable.zip
SHA256SUMS.txt
```

Tag release run is still supported for maintainers who prefer CLI release control:

```bash
git tag v1.2.31
git push origin v1.2.31
```

Both manual runs and tag builds can publish the portable ZIP and checksum to the GitHub release.

## Release checklist

Before publishing a non-draft release:

- `dotnet restore` passes.
- `dotnet build` passes.
- protocol smoke tests pass.
- Windows portable ZIP is created.
- portable app opens from extracted folder.
- CLI help opens from extracted folder.
- README badges render correctly.
- GitHub Pages landing page is online.
- release notes use honest beta/stable wording.
- exported evidence is reviewed for customer-sensitive information.
