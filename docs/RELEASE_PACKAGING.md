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
pwsh ./scripts/publish-windows-portable.ps1 -Version 1.2.30
```

Expected output:

```text
artifacts/release/ArIEC103-v1.2.30-win-x64-portable.zip
artifacts/release/SHA256SUMS.txt
```

Verify package structure:

```powershell
pwsh ./scripts/verify-release-package.ps1 -PackagePath artifacts/release/ArIEC103-v1.2.30-win-x64-portable.zip
```

## GitHub Actions package flow

The workflow below creates the same portable ZIP and checksum:

```text
.github/workflows/release-package.yml
```

Manual run:

1. Open **Actions**.
2. Select **Build Windows portable package**.
3. Run workflow.
4. Download the artifact.
5. Test it on a clean Windows machine.

Tag release run:

```bash
git tag v1.2.30
git push origin v1.2.30
```

For tag builds, the workflow uploads the portable ZIP and checksum to the GitHub release.

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
