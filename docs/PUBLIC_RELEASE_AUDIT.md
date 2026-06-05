# Public Release Audit — v1.2.26

Status: **public-source ready with normal project-owner review recommended before tagging**.

This audit focuses on repository hygiene, Apache-2.0 readiness, third-party notice completeness, and public-facing wording.

## Completed cleanup

- Confirmed top-level `LICENSE` contains Apache License, Version 2.0.
- Kept `PackageLicenseExpression` as `Apache-2.0` in project metadata.
- Added centralized package/repository metadata in `Directory.Build.props`.
- Added SPDX license identifiers to comment-capable source files.
- Rewrote `README.md` as a clean public product overview instead of a long rolling changelog.
- Updated `NOTICE` with concise project, Lucide, and System.IO.Ports attribution.
- Updated `THIRD_PARTY_NOTICES.md` with third-party license details.
- Added `SECURITY.md` to prevent accidental disclosure of sensitive relay/customer data in public issues.
- Removed internal visual wording from current public-facing docs where it could look like an unrelated product dependency.

## Release packaging rules

Do not include these in public ZIPs or GitHub commits:

- `bin/`, `obj/`, `out/`, `dist/`, `node_modules/`
- real field captures, COM logs, PCAP/PCAPNG, customer traces, spreadsheets, PDFs, MSG files
- generated reports or exported evidence from real projects
- secrets, tokens, private endpoints, production config
- font files used only during artwork generation

## Third-party dependency status

| Item | Purpose | License | Repository treatment |
| --- | --- | --- | --- |
| .NET 8 / WPF | runtime/framework | Microsoft/.NET licensing | framework dependency only |
| System.IO.Ports 8.0.0 | serial COM-port I/O | MIT | NuGet package reference |
| Lucide Icons geometry references | WPF action icons | ISC | attributed in `NOTICE` and `THIRD_PARTY_NOTICES.md` |
| IEC / 103 app icon | project branding | project-owned | source asset included |

## Legal/clean-room risk notes

- Keep relay signal names user-owned through mapping profiles.
- Do not paste vendor manual tables into source or docs unless permission is explicit and documented.
- Do not port parser/state-machine code from GPL/commercial IEC stacks.
- Benchmark references in docs must remain descriptive, not copied implementation logic.

## Suggested tag wording

```text
v1.2.26 — Public release hygiene + Apache-2.0 notice audit

This release prepares ArIEC103 for public repository use by cleaning README wording, centralizing package metadata, adding SPDX identifiers, updating NOTICE / THIRD_PARTY_NOTICES, and adding a security disclosure policy. Runtime behavior is unchanged from the previous logo/icon refresh package.
```
