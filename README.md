# ArIEC103

**Current package:** v1.2.3 Responsive AR Layout + Stable Session Header


## v1.2.3 Responsive AR Layout + Stable Session Header

This build audits the desktop output layout against the newer product direction: operator-first views, raw protocol trace as an expert layer, and ARServer-like handmade software identity without visual clipping or header jitter.

Key changes:

- stabilizes the top **Session State** card so it no longer changes size or appears to flicker during high-volume polling
- stops pushing every live protocol state into the top header; detailed state remains in Operator Evidence and Frame Trace
- reduces outer spacing and sidebar width so the workspace behaves more like a responsive web app
- adds fixed-height, text-trimmed status fields to prevent Auto-sized layout jumps
- enables explicit horizontal/vertical scrolling on all large DataGrid views
- tightens TabControl, DataGrid, row hover, card, and typography styling toward the ARServer identity
- keeps raw hex visible in Frame Trace and inspector panels, while the main flow remains operator-readable
- see `docs/RESPONSIVE_LAYOUT_POLICY.md`


## v1.2.2 Diagnostics Pipeline + Serial Timeout Safety

This build converts recoverable runtime faults into selectable/copyable diagnostics instead of allowing them to appear as unhandled workflow exceptions.

Key changes:

- normal serial no-data waits no longer surface as `System.TimeoutException` in the UI workflow
- `SerialByteTransport.ReadAsync` now uses a bounded `BytesToRead` polling loop instead of relying on blocking read timeout exceptions
- added WPF **Diagnostics** tab
- diagnostic rows are selectable and can be copied like a Visual Studio Error List row
- exception type/stack detail is preserved in diagnostic detail
- serial read/write/close errors, mapping profile errors, master faults, and warning evidence are routed into diagnostics
- Markdown report now includes a diagnostics appendix
- see `docs/DIAGNOSTICS_POLICY.md`


## v1.2.1 compile fix and simulator strategy

This build removes the unsupported WPF `TextBlock.CharacterSpacing` setter from `ModernTheme.xaml`. WPF does not expose that property on `System.Windows.Controls.TextBlock`.

For master testing, see `docs/SLAVE_SIMULATOR_STRATEGY.md`. The planned simulator is a supporting tool to validate ArIEC103 as an active IEC-103 master tester: GI response, ACD-driven Class 1 queue, NO DATA behavior, DFC/backoff, relay timestamp event logs, and negative protocol scenarios.



**ArIEC103** is a clean-room Apache-2.0 IEC 60870-5-103 Master Tester and analyzer.

Product direction is locked:

> **Active IEC-103 master tester that connects to protection relays acting as IEC-103 slaves, with SCADA-correct polling, raw frame evidence, user-defined signal mapping, Value Viewer, relay-timestamp Event Log, and engineering findings.**

It is intentionally **single connection first**. Dual redundancy is not part of this product baseline.

## v1.2 Stop-Safe UI + AR Software Identity

v1.2 fixes a real desktop workflow bug and aligns the app/landing visual language with the mature AR handmade software identity used by ARServer.

Runtime and UX changes:

- fixed Start/Stop toggle behavior after a user stop request
- Stop now cancels the session and actively closes the transport so blocking serial reads are released
- Stop button remains available as a force-close action while the session is shutting down
- serial read path uses timeout-aware `SerialPort.Read` rather than relying only on `BaseStream.ReadAsync` cancellation
- WPF theme refreshed with ARServer-style cards, soft buttons, segmented tabs, modern DataGrid rows, and slim scrollbars
- focus rectangles/noisy dotted visual artifacts removed from the desktop cockpit
- landing page redesigned to match the ARServer product identity: cinematic hero card, glass session panel, architecture map, field-truth panel, and restrained typography

Product rule remains unchanged: ArIEC103 is an active IEC-103 master tester first; raw frame trace is the expert evidence layer, not the main language for normal users.

## v1.1 Operator Output + Performance Buffer

v1.1 aligns the software output with the product direction: the main user experience is readable engineering evidence, while raw hex stays available as an advanced protocol transparency layer.

New runtime capability:

- **Operator Evidence** tab with readable meaning/action columns for users who do not read raw hex
- dedicated **Frame Trace** tab for raw TX/RX protocol transparency
- evidence event fields for operator message, protocol meaning, and recommended action
- WPF queued/batched UI rendering to avoid per-frame render storms
- bounded visible rolling windows for evidence trace, relay event log, findings, and session notes
- engine-side retained evidence buffers for long sessions
- Markdown report now documents retained evidence buffer behavior
- `docs/OUTPUT_AND_PERFORMANCE_POLICY.md` added

Output hierarchy is now locked:

```text
Operator Evidence  -> readable engineering meaning
Value Viewer       -> current relay snapshot
Relay Event Log    -> relay-timestamp edge/state changes
Frame Trace        -> raw hex and protocol transparency
```


## v1.0 Field MVP Assessment Layer

v1.0 adds an **AutoTest-style assessment** layer for active master sessions. ArIEC103 now turns raw session evidence into a FAT/SAT-oriented checklist while keeping raw frames visible.

New runtime capability:

- overall assessment status and score
- WPF **AutoTest Assessment** tab
- CLI assessment summary and checklist
- Markdown report **AutoTest assessment** section
- checks for communication activity, FT1.2 frame quality, GI completion, SCADA-style polling, timeout behavior, value acquisition, relay timestamp quality, mapping coverage, and findings

Assessment status does not replace engineering judgment. It is a review aid built from the captured evidence.

## v0.9 Documentation / Governance Update

v0.9 locks the mature product direction and adds guardrails for future coding work. Runtime behavior remains based on v0.8, but the repository now documents the final target architecture and agent rules.

New governance documents:

- `AGENTS.md` — hard rules for future coding agents/maintainers
- `docs/PRODUCT_BENCHMARK_AND_STRATEGY.md` — external benchmark and product strategy
- `docs/PROJECT_STRUCTURE_FINAL.md` — mature source tree and responsibility boundaries
- `docs/MASTER_POLLING_POLICY.md` — Class 2 normal / ACD-driven Class 1 policy
- `docs/EVENT_LOG_POLICY.md` — relay timestamp and edge-event logging rules
- `docs/CLEAN_ROOM_POLICY.md` — Apache-2.0 clean-room development policy
- `docs/ROADMAP.md` — v1.0 to v1.4 roadmap

## What v0.8 Adds

v0.8 corrects the product architecture around signal naming and event handling:

- no built-in vendor or generic signal profile is used for final signal names
- added **user mapping profile** JSON schema/config
- added sample editable mapping file under `samples/mapping-profiles/`
- WPF can import/clear a mapping profile
- CLI supports `--mapping <profile.json>`
- evidence now carries mapped signal name/value only if a user profile is loaded
- added **Value Viewer** snapshot from incoming relay values/events
- added **Relay Event Log** that uses the **relay timestamp** from IEC-103 ASDU time fields, not PC arrival time
- Event Log records **state change** or **spontaneous/edge event** from the relay, not every incoming GI/status frame
- raw FUN/INF remains visible beside every mapped signal name

The active master engine is clean-room and does not use lib60870, Open103, Wireshark code, or any third-party IEC protocol stack.

`System.IO.Ports` is used only for serial communication plumbing, not protocol logic.

## Build

Requirements:

- .NET 8 SDK
- Visual Studio 2022/2026 or command line `dotnet`

```bash
dotnet restore
dotnet build
```

## Run WPF Desktop Master Tester

```bash
dotnet run --project src/ArIEC103.Desktop
```

Or on Windows:

```bat
RUN_DESKTOP.bat
```

Workflow:

```text
Select target mode
Select COM/baud/address
Optionally browse user mapping profile
Start Test
Watch Operator Evidence / Frame Trace / Value Viewer / Event Log / AutoTest Assessment / Findings
Export Evidence Report
```

## Run Demo Master Without Relay

Use this first to validate the product workflow without hardware:

```bash
dotnet run --project src/ArIEC103.Cli -- master --simulate --duration 10 --mapping samples/mapping-profiles/example-user-mapping.profile.json --report out/demo-master-evidence.md --json out/demo-master-evidence.json
```

Or on Windows:

```bat
RUN_DEMO_MASTER.bat
```

## Run Active IEC-103 Master CLI

Edit COM port and addresses before connecting to a real relay:

```bash
dotnet run --project src/ArIEC103.Cli -- master --port COM1 --baud 9600 --link 1 --ca 1 --duration 30 --mapping samples/mapping-profiles/example-user-mapping.profile.json --report out/master-evidence.md --json out/master-evidence.json
```

Common options:

```text
--simulate
--mapping <profile.json>
--port COM1
--baud 9600
--databits 8
--parity Even
--stopbits 1
--link 1
--ca 1
--duration 30
--timeout 1500
--class2-interval 500
--max-class1-drain 64
--no-gi
--clock-sync
--reset-link
--no-reset-fcb
--timeout-burst 3
--no-timeout-reset
--report out/master-evidence.md
--json out/master-evidence.json
```

## Run Offline Analyzer

```bash
dotnet run --project src/ArIEC103.Cli -- analyze samples/sample_iec103_trace.log --report out/report.md --json out/report.json
```

## User Mapping Profile

ArIEC103 does not ship relay-specific signal names. Users provide mapping based on their relay/project database.

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

If mapping is loaded, the app displays:

```text
Breaker Position | Closed | FUN 192 / INF 36 | relay timestamp
```

If mapping is not loaded, the app displays raw evidence:

```text
FUN 192 / INF 36 | DPI=2 | relay timestamp
```

## Event Log Rule

Event Log is not a raw frame dump. It records relay edge/state-change evidence:

```text
- Event time = relay timestamp from ASDU time field
- PC arrival time is stored only as forensic metadata
- GI/status snapshots update Value Viewer
- State changes update Event Log
- Spontaneous relay events update Event Log
```

This prevents the event list from becoming noisy during GI or cyclic polling.

## Master Polling Policy

Bad policy:

```text
Request Class 1
NO DATA
Request Class 1
NO DATA
Request Class 1
NO DATA
```

ArIEC103 policy:

```text
On connect:
  Optional Reset Remote Link
  Reset FCB
  Optional Clock Sync
  Optional GI
  Bounded GI Class 1 follow-up

Normal runtime:
  Request Class 2 at configured interval

When slave indicates ACD=1:
  Drain Class 1 until NO DATA / GI END / ACD clear / DFC busy / drain limit

When slave indicates DFC=1:
  Back off before retry

When no response:
  Raise timeout evidence
  Recover only after configured timeout burst
  Do not blindly flood Class 1
```

This mirrors healthy SCADA master behavior: **Class 1 is for pending high-priority/event data, not a continuous empty polling loop.**

## Current Architecture

```text
ArIEC103.Core
  FT1.2 parser
  link control decoder
  IEC-103 ASDU starter decoder
  user mapping profile schema/loader
  offline trace analyzer
  Markdown/JSON report

ArIEC103.Master
  serial transport
  simulated generic relay/demo slave transport
  FT1.2 frame builder
  active master state machine
  ACD-driven polling policy
  bounded GI follow-up
  timeout recovery
  live evidence events
  Value Viewer model
  relay-timestamp Event Log model
  deterministic IEC-103 slave simulator for master testing
  master Markdown/JSON report

ArIEC103.Cli
  analyze command
  master command
  slave simulator command
  --mapping profile support

ArIEC103.Desktop
  WPF master tester shell
  COM setup
  mapping profile import/clear
  live evidence grid
  Value Viewer
  Relay Event Log
  AutoTest assessment checklist
  finding cards/table
  Markdown report export
```

## IEC-103 Slave Simulator

ArIEC103 now includes a deterministic slave simulator for validating the master engine before testing with a physical relay. It is a supporting test tool, not the main product direction.

```bat
dotnet run --project src\ArIEC103.Cli -- slave --port COM2 --baud 9600 --link 1 --ca 1 --duration 300
```

Use it with a virtual null-modem pair or paired USB-RS485 adapters, then run the master on the opposite COM port. The simulator supports GI, Class 1 drain, Class 2 ACD status, NO DATA when queue is empty, and negative modes such as `--missing-gi-end`, `--dfc-busy`, `--silent`, and `--bad-checksum`.

See `docs/SLAVE_SIMULATOR_USER_GUIDE.md`.

## GitHub Repository Hygiene

This repository is configured to keep GitHub clean: source code, documentation, legal/config files, sanitized samples, and the static landing page are allowed; generated binaries, reports, field captures, packages, and secrets are ignored.

See `docs/GITHUB_REPOSITORY_HYGIENE.md`.

## License

Apache-2.0.

This repository is intended to be legally clean and corporate-friendly. It does not include code from commercial or GPL IEC 60870 protocol stacks.

## Clean-Room Policy

- Do not copy code from commercial, GPL, or third-party protocol stacks.
- Do not port internal class structures from other libraries.
- Use public protocol behavior, independently written code, and legally shareable test vectors.
- Keep all third-party dependencies documented in `THIRD_PARTY_NOTICES.md`.

## v1.2.5 layout/diagnostics note

- Keep operator views readable first; raw hex stays in Frame Trace and Inspector.
- Wide protocol columns, horizontal scroll, ellipsis, and tooltip are preferred over visually clipped text.
- Serial close/dispose driver exceptions must be converted into Diagnostics evidence, not allowed to interrupt Stop/Close workflow.

### v1.2.6 — XAML Resource Setter Compile Fix

- Fixes a WPF `XamlParseException` caused by an invalid `Resources` style setter in `ModernTheme.xaml`.
- Keeps v1.2.5 visual fit, diagnostics, and serial-close robustness intact.


### v1.2.7 commissioning output fit

The desktop output is now operator-first: repetitive polling noise is suppressed from Operator Evidence, Frame Trace shows real TX/RX raw frames only, and primary grids are sized to avoid routine horizontal scrolling. Raw hex remains available for expert protocol transparency, but selected-row inspectors carry long detail instead of forcing every column into the main grid.


## v1.2.8 Protection relay simulator behavior

The IEC-103 slave simulator now includes a deterministic protection-relay behavior model for master testing:

- random phase pickup for phase A, B, or V/C,
- trip ON about 200 ms after pickup,
- pickup/trip remain latched ON until reset,
- reset by Type 20 command address FUN=255 / INF=19 by default or auto reset after 20 seconds,
- next pickup/trip cycle after 10 seconds,
- animated Class 2 current measurands for Value Viewer testing.

Run the slave simulator with:

```bat
dotnet run --project src\ArIEC103.Cli -- slave --port COM2 --baud 9600 --link 1 --ca 1 --duration 300
```

For a readable demo in the master UI/report, load `samples/mapping-profiles/example-user-mapping.profile.json`.

## v1.2.9 UI output direction

The WPF shell now uses a more compact AR-style header, lighter premium typography, subtle gradient cards, and a commissioning-first Frame Trace grid. Raw hex remains preserved as selected-frame evidence, while the primary grid prioritizes readable engineering meaning.



## v1.2.10 UI refinement

The WPF cockpit now uses a lighter Aptos-centered typography system, more compact header/KPI cards, and a collapsible Frame Trace inspector. Readable protocol explanation is the primary view; raw frame evidence remains available on demand for expert audit.


## v1.2.11 update

- Frame Trace inspector now uses a meaning-first raw hex map. Hovering hex blocks explains the selected byte/field so users can understand the frame without reading raw hex manually.
- Session state and sidebar spacing were tightened to avoid clipping and hover cut-off.

### v1.2.12 — ASE-style hex group map

The Frame Trace inspector now groups raw hex into IEC-103 protocol blocks and highlights the matching meaning row on hover. This keeps the explanation panel stable while giving expert users full protocol transparency without forcing commissioning users to read individual bytes manually.


## v1.2.14 Line Monitor Compile Fix

- Fixes missing `ProtocolMapHintText` code-behind reference from v1.2.13.
- Keeps Line Monitor Pro grouped hex/interpreter behavior intact.


## v1.2.15 UI rule
- Line Monitor rows include a compact `Frame` column for raw hex evidence.
- The selected-frame interpreter must be readable as compact protocol cards, not a nested scrolling text wall.
- Raw hex grouping remains field-aware: FT1.2, control/link, ASDU header, FUN/INF, value/time, integrity.

## v1.2.16 Line Monitor Rule

Frame Trace must behave like a real IEC-103 line monitor: TX/RX transaction tracking on the left and selected frame interpretation on the right. Do not return to a bottom-only interpreter or hover-only explanation. Raw hex remains visible as evidence, but the selected frame's IEC-103 meaning must be readable without scrolling away from the trace.



## v1.2.17 Instrument Cockpit Lock

- Main screen is a protocol observation cockpit, not a configuration form.
- Connection and polling parameters belong in the Setup overlay.
- Header uses compact chips and LED activity indicators, not large KPI cards.
- Default master session is continuous monitoring until Stop; optional timeout is a setup parameter.
- UI font must remain Aptos-only; do not use monospace fonts for raw hex.

### v1.2.18 UI/runtime guardrail
- All WPF DataGrid monitor surfaces must remain `IsReadOnly=True`; they are evidence views, not editors.
- Do not use text-only command rail buttons where an icon + tooltip is clearer.
- Line monitor must prioritize readable transaction tracking and avoid routine horizontal scrolling.


## v1.2.19 Update

- All grids support multi-select and tab-separated copy.
- Selected tab data can be exported as `.txt`.
- Value Viewer uses stable ordering: digital/status first, measurands below.
- Event Log shows SOE date and time.


See also: `docs/CLASS1_CLASS2_AUDIT.md` for the Class 1/Class 2 transaction classification rules.

### v1.2.21 UI motion and button palette
- Segmented navbar uses a bright fluid pill treatment, not a grey selected state.
- Hover grows subtly and click compresses subtly to make navigation feel tactile.
- Primary buttons use a brighter modern blue/cyan accent with dark readable captions.


## v1.2.22

- Fixed invalid WPF TabItem `IsPressed` trigger in ModernTheme.xaml.


## v1.2.23 UI cockpit guardrail

- Use the custom segmented navbar with a real sliding pill; do not rely on default TabItem selected gray highlight.
- Left rail buttons must use icon + small caption with safe hover/shadow margins.
- ComboBox controls must use the modern rounded web-style template and stay visually aligned with TextBox fields.
- Do not use invalid WPF control properties such as TabItem.IsPressed.
