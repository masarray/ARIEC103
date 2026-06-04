# ArIEC103 Roadmap

## Current: v1.2.3 Responsive AR Layout

- Stable top Session State header.
- ARServer-aligned workspace layout.
- DataGrid scrolling/virtualization maintained for high-volume polling.
- Raw frame trace remains advanced transparency layer, not the main user workflow.


## Current v1.2.2

ArIEC103 now includes stop-safe desktop session control, ARServer-aligned WPF styling, operator-first output, advanced Frame Trace, Value Viewer snapshot, relay timestamp Event Log, user mapping profile, WPF AutoTest Assessment tab, queued/batched UI rendering, bounded retained evidence, Markdown assessment report, documented IEC-103 slave simulator strategy, and a diagnostics pipeline. v1.2.2 fixes the WPF `CharacterSpacing` compile issue and prevents normal serial no-data waits from surfacing as unhandled `System.TimeoutException`; recoverable exceptions now become selectable/copyable Diagnostics rows.


## v0.9 — Product Architecture and Agent Governance

- Add `AGENTS.md`
- Lock product direction as Active Master Tester + Analyzer
- Add final project structure guide
- Add benchmark/strategy notes
- Add polling/event-log/clean-room policies
- Update README and landing direction

## v1.0 Field MVP

Target: usable IEC-103 master tester for one relay connection.

Required:

- stable serial master connection
- Reset FCB / optional Reset Link
- Clock Sync command
- GI command
- Class 2 normal polling
- ACD-driven Class 1 event drain
- raw frame monitor
- ASDU decode: Type, COT, CA, FUN, INF, DPI/value, relay timestamp
- user mapping profile import
- Value Viewer
- Relay Event Log using relay timestamp
- findings panel
- Markdown/JSON/HTML evidence export
- deterministic demo relay workflow

## v1.1 Operator Output and Performance Buffer

- operator-first evidence output
- advanced Frame Trace raw protocol transparency
- WPF queued/batched UI rendering
- bounded visible rolling windows
- engine retained evidence limits
- report retention counters

## v1.2 Stop-Safe UI and AR Software Identity

- fix Start/Stop toggle after stop request
- close active transport to release blocking serial reads/writes
- add timeout-aware SerialPort.Read path
- align WPF theme with ARServer visual identity
- align landing page with ARServer product identity

## v1.3 Standalone IEC-103 Slave Simulator

- standalone basic IEC-103 slave simulator for testing the master
- ACK for Reset FCB / Reset Remote Link
- GI response with bounded Class 1 queue
- Type 1 / Type 2 DPI event generation
- Type 5 identification
- Type 8 GI termination
- NO DATA after queue drained
- ACD=1 only while Class 1 data is pending
- negative test scenario foundation: timeout, DFC, bad checksum, missing GI END

## v1.4 Master Hardening with Simulator Scenarios

- deterministic scenario regression tests
- no Class 1 bombardment validation
- DFC backoff validation
- relay timestamp Event Log validation
- state-change event de-duplication validation

## v1.5 AutoTest / FAT Scenario Runner

- scenario config
- expected mapped points
- GI completeness check
- clock sync test
- Class 1 drain test
- timeout/DFC behavior checks
- pass/fail summary

## v1.6 Professional Reports

- HTML evidence report
- PDF report
- report table of contents
- evidence links from findings to frames
- export evidence bundle

## v1.7 Protocol Coverage Expansion

- deeper measurand handling
- Type 10/11 generic ASDU handling
- Type 23–31 disturbance-transfer visibility
- vendor/private ASDU raw container
- user extension hook for project-specific decode

## Deferred / Not Baseline

- dual redundancy
- IEC-101/104 expansion inside this repo
- built-in vendor signal databases
- official conformance certification claims
- proprietary relay mapping bundle

## Diagnostics pipeline note

Runtime faults and recoverable transport exceptions are routed to Diagnostics rows. The UI must remain operator-safe: no unhandled serial timeout should block the Start/Stop workflow, and every exception worth escalating must be selectable/copyable from the Diagnostics tab.


## v1.2.4 - Slave Simulator Foundation

- Fix WPF DataGrid row trigger compile issue.
- Add deterministic IEC-103 slave simulator CLI mode.
- Use simulator as regression harness for master polling policy.

## Next

- Add WPF launcher/help panel for slave simulator setup.
- Add simulator scenario files for normal GI, missing GI END, DFC busy, timeout/no response, and bad checksum.
- Add automated master-vs-simulator regression tests when test infrastructure is introduced.


## v1.2.5 layout/diagnostics note

- Keep operator views readable first; raw hex stays in Frame Trace and Inspector.
- Wide protocol columns, horizontal scroll, ellipsis, and tooltip are preferred over visually clipped text.
- Serial close/dispose driver exceptions must be converted into Diagnostics evidence, not allowed to interrupt Stop/Close workflow.


## v1.2.8

- Protection relay behavior simulator.
- Random pickup phase, delayed trip, latch reset, auto reset, repeated cycles.
- Animated current measurands for master/value-viewer testing.

## v1.2.9 UI output direction

The WPF shell now uses a more compact AR-style header, lighter premium typography, subtle gradient cards, and a commissioning-first Frame Trace grid. Raw hex remains preserved as selected-frame evidence, while the primary grid prioritizes readable engineering meaning.

## v1.2.16 Line Monitor Rule

Frame Trace must behave like a real IEC-103 line monitor: TX/RX transaction tracking on the left and selected frame interpretation on the right. Do not return to a bottom-only interpreter or hover-only explanation. Raw hex remains visible as evidence, but the selected frame's IEC-103 meaning must be readable without scrolling away from the trace.



## v1.2.17 Instrument Cockpit Lock

- Main screen is a protocol observation cockpit, not a configuration form.
- Connection and polling parameters belong in the Setup overlay.
- Header uses compact chips and LED activity indicators, not large KPI cards.
- Default master session is continuous monitoring until Stop; optional timeout is a setup parameter.
- UI font must remain Aptos-only; do not use monospace fonts for raw hex.
