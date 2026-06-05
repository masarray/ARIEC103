# ArIEC103

**ArIEC103** is an Apache-2.0 clean-room IEC 60870-5-103 active master tester and analyzer for protection relay communication testing.

It connects to protection relays / IEDs acting as IEC-103 slaves, runs SCADA-correct master polling, decodes the response, keeps raw frame evidence available, and presents the result as readable engineering output for FAT, SAT, commissioning, and troubleshooting.

> Current public release package: **v1.2.26 — Public release hygiene + Apache-2.0 notice audit**

## What this tool is for

ArIEC103 is built for engineers who need to prove what happened on an IEC-103 serial link, not just see that bytes moved.

Core use cases:

- active IEC-103 master testing against one protection relay / IED slave
- SCADA-style Class 2 background polling with ACD-driven Class 1 event drain
- General Interrogation startup verification
- relay timestamped event review
- user-owned signal mapping from FUN/INF to project signal names
- readable operator evidence plus raw frame trace transparency
- Markdown/JSON evidence export for review, FAT/SAT records, and troubleshooting notes

## Product boundary

ArIEC103 is intentionally focused:

- single IEC-103 connection first
- active master tester first
- offline trace analyzer as a supporting mode
- user mapping profiles instead of built-in guessed vendor profiles
- raw FUN/INF/Type/COT/DPI/frame evidence always preserved
- no copied source code from commercial, GPL, or unclear-license protocol stacks

It is **not** a vendor-specific tester, not a dual-redundancy IEC-101 clone, and not a wrapper around third-party IEC protocol stack source code.

## Main capabilities

### Desktop WPF tester

- COM port setup for IEC-103 serial communication
- continuous master session until Stop
- operator evidence grid
- Line Monitor / Frame Trace view
- Value Viewer snapshot
- relay timestamped Event Log
- AutoTest-style assessment checklist
- Diagnostics tab for recoverable runtime issues
- Markdown evidence export

### CLI tools

- active master runner
- offline trace analyzer
- deterministic slave simulator for validating the master engine
- Markdown and JSON report output

### Protocol behavior

The master polling policy is intentionally conservative and SCADA-like:

```text
Startup:
  Open transport
  Optional startup delay
  Optional Reset Remote Link
  Reset FCB
  Optional Clock Sync
  Optional General Interrogation
  Bounded GI Class 1 follow-up

Normal runtime:
  Poll Class 2 at configured interval

If ACD=1:
  Drain Class 1 until NO DATA / GI END / ACD clear / DFC busy / max drain / timeout

If DFC=1:
  Back off and record busy evidence

If timeout:
  Raise evidence and recover only after the configured timeout burst
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

## Quick start

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

## User mapping profile

ArIEC103 does not ship official relay-specific signal names. The application can decode IEC-103 protocol evidence, but project signal naming belongs to the user through a mapping profile.

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

## Architecture

```text
ArIEC103.Core
  FT1.2 parser
  link control decoder
  IEC-103 ASDU starter decoder
  user mapping profile schema/loader
  offline trace analyzer
  Markdown/JSON report primitives

ArIEC103.Master
  serial transport
  deterministic simulated relay transport
  FT1.2 frame builder
  active master state machine
  ACD-driven polling policy
  bounded GI follow-up
  timeout recovery
  live evidence events
  Value Viewer model
  relay timestamp Event Log model
  master Markdown/JSON report

ArIEC103.Cli
  analyze command
  master command
  slave simulator command
  mapping profile support

ArIEC103.Desktop
  WPF master tester shell
  setup overlay
  live evidence views
  Value Viewer
  Relay Event Log
  AutoTest assessment
  Diagnostics
  report export
```

WPF is intentionally kept as a shell over the engine. Protocol state, FCB/FCV handling, Class 1 drain decisions, ASDU parsing, and event classification belong in `ArIEC103.Master` and `ArIEC103.Core`.

## Repository hygiene

This repository is configured for public source release:

- source code, docs, legal/config files, workflows, sanitized samples, and the static landing page are allowed
- build output, packages, reports, field captures, spreadsheets, PDFs, PCAP files, secrets, and local IDE state are ignored
- real customer / utility / vendor captures must not be committed unless fully sanitized and legally shareable

See:

- `docs/GITHUB_REPOSITORY_HYGIENE.md`
- `docs/CLEAN_ROOM_POLICY.md`
- `docs/PUBLIC_RELEASE_AUDIT.md`

## Third-party components

Runtime/framework references are documented in `THIRD_PARTY_NOTICES.md`.

Current notable third-party items:

- .NET 8 / `System.IO.Ports` for serial COM-port I/O
- Lucide-style outline icon geometry references for the WPF command rail

The `IEC / 103` software icon is project-owned raster artwork.

## License

ArIEC103 is released under the **Apache License, Version 2.0**. See `LICENSE`.

Copyright 2026 Ari Sulistiono.

Unless explicitly stated otherwise, contributions submitted to this repository are provided under Apache-2.0. See `CONTRIBUTING.md`.
