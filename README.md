# ArIEC103

**ArIEC103** is an Apache-2.0 IEC 60870-5-103 active master tester and analyzer for protection relay communication checks.

It connects to one IEC-103 slave relay, runs a controlled master session, decodes relay responses, keeps raw TX/RX evidence available, and presents the result as readable engineering output for FAT, SAT, commissioning, and troubleshooting.

> Current public release package: **v1.2.27 — protocol retry safety, evidence privacy, CI smoke tests, and user-facing documentation refresh**

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

1. Download the Windows release package from GitHub Releases.
2. Open **ArIEC103.Desktop**.
3. Click **Setup**.
4. Select the COM port and serial settings used by the relay.
5. Set the relay **Link Address** and **Common Address**.
6. Keep **Reset FCB** enabled for normal startup synchronization.
7. Enable **General Interrogation** when you want a startup snapshot.
8. Load a mapping profile when you want readable signal names.
9. Click **Start**.
10. Review **Operator Evidence**, **Value Viewer**, **Relay Event Log**, and **Diagnostics**.
11. Export Markdown evidence when you need a reviewable test record.

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
