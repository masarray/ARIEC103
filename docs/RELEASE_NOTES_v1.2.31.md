# ArIEC103 v1.2.31 - Manual Release Publishing

This release improves the GitHub release flow for public beta distribution.

## Highlights

- Manual GitHub Actions run can now create or update a GitHub Release.
- Manual workflow inputs now include version, publish release, pre-release, draft, and release notes file.
- Tag-based release publishing is still supported.
- Release assets are uploaded with a consistent portable ZIP name and SHA256 checksum.
- Release packaging documentation now explains the manual release flow clearly.

## Release assets

```text
ArIEC103-v1.2.31-win-x64-portable.zip
SHA256SUMS.txt
```

## Recommended release setting

For the current project maturity, keep the GitHub Release marked as **Pre-release** until the relay validation matrix and ASDU test vectors are stronger.

## Validation reminder

Before promoting any release as stable:

- Confirm `dotnet restore` passes.
- Confirm `dotnet build` passes.
- Confirm protocol smoke tests pass.
- Test the portable package on a clean Windows machine.
- Validate with at least one known IEC-103 relay or trusted simulator.
- Review exported evidence before sharing externally.
