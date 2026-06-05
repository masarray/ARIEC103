# ArIEC103

[![Build](https://github.com/masarray/ARIEC103/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/masarray/ARIEC103/actions/workflows/ci.yml)
[![Pages](https://github.com/masarray/ARIEC103/actions/workflows/pages.yml/badge.svg?branch=main)](https://github.com/masarray/ARIEC103/actions/workflows/pages.yml)
[![Package](https://github.com/masarray/ARIEC103/actions/workflows/release-package.yml/badge.svg)](https://github.com/masarray/ARIEC103/actions/workflows/release-package.yml)
[![Release](https://img.shields.io/github/v/release/masarray/ARIEC103?include_prereleases&label=release)](https://github.com/masarray/ARIEC103/releases)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache--2.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%20desktop-0078D4.svg)](#windows-desktop-tester)

**ArIEC103** is an Apache-2.0 IEC 60870-5-103 active master tester and analyzer for protection relay communication checks.

It connects to one IEC-103 slave relay, runs a controlled master session, decodes relay responses, keeps raw TX/RX evidence available, and presents the result as readable engineering output for FAT, SAT, commissioning, and troubleshooting.

> Current public release package: **v1.2.30 — release packaging hardening, portable Windows ZIP workflow, quick-start/troubleshooting docs, validation matrix, and status badges**

## Who this tool is for

ArIEC103 is for protection, SCADA, commissioning, and panel/FAT engineers who need to answer practical questions:

- Is the relay answering on the selected COM port and link address?
- Does General Interrogation start and finish cleanly?
- Are Class 1 events requested only when the relay indicates pending event data?
- What value/status did the relay send, and what was the relay timestamp?
- Can the test evidence be exported for FAT/SAT records or troubleshooting review?

## Main features

### Windows desktop tester

- COM port setup for IEC-103 serial communication.
- Active master session against one relay/IED slave.
- Setup overlay for baudrate, link address, common address, timeout, GI, and polling behavior.
- Operator Evidence grid for readable session activity.
- Line Monitor / Frame Trace view for raw TX/RX frame inspection.
- Value Viewer snapshot for the latest decoded relay points.
- Relay Event Log for relay timestamped state changes and events.
- AutoTest-style assessment checklist.
- Diagnostics tab for recoverable runtime issues.
- Markdown evidence export.

### Command-line tools

- Active master runner for repeatable bench tests.
- Offline trace analyzer for existing IEC-103 logs.
- Deterministic slave simulator for validating the master engine without relay hardware.
- Markdown and JSON report output.

### User-owned signal mapping

ArIEC103 decodes IEC-103 protocol fields such as Type, COT, FUN, INF, DPI/value, timestamp, checksum, and raw frame bytes.

Readable project signal names come from your own JSON mapping profile. This avoids guessed vendor naming and keeps FAT/SAT evidence aligned with the approved project signal list.

## How to use the desktop app

1. Download the Windows portable package from GitHub Releases.
2. Extract the ZIP to a local folder.
3. Run `Start-ArIEC103.bat`.
4. Click **Setup**.
5. Select the COM port and serial settings used by the relay.
6. Set the relay **Link Address** and **Common Address**.
7. Keep **Reset FCB** enabled for normal startup synchronization.
8. Enable **General Interrogation** when you want a startup snapshot.
9. Load a mapping profile when you want readable signal names.
10. Click **Start**.
11. Review **Operator Evidence**, **Value Viewer**, **Relay Event Log**, and **Diagnostics**.
12. Export Markdown evidence when you need a reviewable test record.

## Master polling behavior

ArIEC103 uses a conservative master policy suitable for relay testing:

```text
Startup:
  Open transport
  Optional startup delay
  Optional reset remote link
  Reset FCB
  Optional clock sync
  Optional General Interrogation
  Bounded GI follow-up

Normal runtime:
  Poll Class 2 at the configured interval

If ACD=1:
  Drain Class 1 until NO DATA / GI END / ACD clear / DFC busy / max drain / timeout

If DFC=1:
  Back off and record busy evidence

If timeout or invalid response:
  Keep FCB state stable, record diagnostic evidence, and recover according to the configured timeout policy
```

Bad behavior deliberately avoided:

```text
Request Class 1
NO DATA
Request Class 1
NO DATA
Request Class 1
NO DATA
```

Class 1 is treated as pending high-priority/event data, not as a blind continuous polling loop.

## Quick start for developers

Requirements:

- .NET 8 SDK
- Windows for the WPF desktop app
- Visual Studio 2022/2026 or command line `dotnet`

Build:

```bash
dotnet restore
dotnet build
```

Run WPF desktop:

```bash
dotnet run --project src/ArIEC103.Desktop
```

Or on Windows:

```bat
RUN_DESKTOP.bat
```

Run a simulated master session without hardware:

```bash
dotnet run --project src/ArIEC103.Cli -- master --simulate --duration 10 --mapping samples/mapping-profiles/example-user-mapping.profile.json --report out/demo-master-evidence.md --json out/demo-master-evidence.json
```

Run active master against a real relay:

```bash
dotnet run --project src/ArIEC103.Cli -- master --port COM1 --baud 9600 --link 1 --ca 1 --duration 30 --mapping samples/mapping-profiles/example-user-mapping.profile.json --report out/master-evidence.md --json out/master-evidence.json
```

Run offline analyzer:

```bash
dotnet run --project src/ArIEC103.Cli -- analyze samples/sample_iec103_trace.log --report out/report.md --json out/report.json
```

Run deterministic slave simulator:

```bash
dotnet run --project src/ArIEC103.Cli -- slave --port COM2 --baud 9600 --link 1 --ca 1 --duration 300
```

Run protocol smoke tests:

```bash
dotnet run --project tests/ArIEC103.Protocol.Tests
```

## Release package

Create a Windows x64 portable package:

```powershell
pwsh ./scripts/publish-windows-portable.ps1 -Version 1.2.30
```

Expected output:

```text
artifacts/release/ArIEC103-v1.2.30-win-x64-portable.zip
artifacts/release/SHA256SUMS.txt
```

Verify the package structure:

```powershell
pwsh ./scripts/verify-release-package.ps1 -PackagePath artifacts/release/ArIEC103-v1.2.30-win-x64-portable.zip
```

GitHub Actions also provides a **Build Windows portable package** workflow for release artifacts.

Useful operator documents:

- `docs/QUICK_START.md`
- `docs/TROUBLESHOOTING.md`
- `docs/VALIDATION_MATRIX.md`
- `docs/RELEASE_PACKAGING.md`

## User mapping profile

Example:

```json
{
  "schema": "ariec103-mapping-profile-v1",
  "profileName": "Project A Feeder 01",
  "deviceName": "Relay Bay 01",
  "linkAddress": 1,
  "commonAddress": 1,
  "signals": [
    {
      "id": "bay01.breaker.position",
      "fun": 192,
      "inf": 36,
      "type": "DPI",
      "name": "Breaker Position",
      "group": "Switchgear",
      "stateMap": {
        "1": "Open",
        "2": "Closed"
      }
    }
  ]
}
```

If mapping is loaded, the app can display:

```text
Breaker Position | Closed | FUN 192 / INF 36 | relay timestamp
```

If mapping is not loaded, the app keeps raw protocol evidence visible:

```text
FUN 192 / INF 36 | DPI=2 | relay timestamp
```

## Evidence privacy

Public evidence reports should not expose local workstation folders or customer file paths.

By default, ArIEC103 exports only the mapping profile file name, not the full local path. Full local paths are reserved for private debugging only.

## Product boundary

ArIEC103 is intentionally focused:

- one IEC-103 connection first
- active master tester first
- offline trace analyzer as a supporting mode
- user mapping profiles instead of guessed vendor profiles
- raw FUN/INF/Type/COT/DPI/frame evidence always preserved
- no built-in vendor-specific signal database

It is not a vendor-specific tester, not a multi-protocol SCADA gateway, and not a replacement for formal site acceptance procedures.

## Next hardening roadmap

ArIEC103 should move from public beta to field-trusted release in this order:

1. **Release hardening** — keep CI green, publish a Windows portable package, add checksums, and keep README / landing page status visible. Initial packaging workflow is now included.
2. **Field validation** — build a relay/simulator validation matrix, add sanitized IEC-103 capture test vectors, and expand ASDU decoder coverage.
3. **Operator workflow** — add profile save/load, serial health diagnostics, and a guided test checklist for first-time users.
4. **FAT evidence** — add one-click formatted PDF report with session metadata, findings, raw evidence appendix, and pass/fail summary.
5. **Analyzer maturity** — add capture replay, compare sessions, stronger filters, and safer long-duration test handling.

See `docs/ROADMAP.md` for the detailed staged plan.

## Repository hygiene

This repository is configured for public source release:

- source code, docs, legal/config files, workflows, sanitized samples, and the static landing page are allowed
- build output, packages, reports, field captures, spreadsheets, PDFs, PCAP files, secrets, and local IDE state are ignored
- real customer / utility / vendor captures must not be committed unless fully sanitized and legally shareable

See:

- `docs/GITHUB_REPOSITORY_HYGIENE.md`
- `docs/CLEAN_ROOM_POLICY.md`
- `docs/PUBLIC_RELEASE_AUDIT.md`

## License

ArIEC103 is released under the **Apache License, Version 2.0**. See `LICENSE`.
