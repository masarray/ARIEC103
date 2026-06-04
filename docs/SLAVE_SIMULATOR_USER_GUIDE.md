# ArIEC103 IEC-103 Slave Simulator

ArIEC103 is primarily an **active IEC-103 master tester**. The slave simulator is a supporting test environment so the master engine can be validated without a physical protection relay.

## Purpose

Use the simulator to validate that the master:

- opens a serial connection,
- sends reset / startup commands,
- sends General Interrogation,
- performs normal Class 2 polling,
- switches to Class 1 event-drain only when ACD=1,
- stops Class 1 drain when the queue is empty / NO DATA is returned,
- records frame trace, event log, diagnostics, and assessment evidence,
- displays signal names and current values when a user mapping profile is loaded.

## Basic serial test setup

Use two COM ports connected by a virtual null modem pair or two USB-RS485 adapters wired together.

Terminal A:

```bat
dotnet run --project src\ArIEC103.Cli -- slave --port COM2 --baud 9600 --link 1 --ca 1 --duration 300
```

Terminal B:

```bat
dotnet run --project src\ArIEC103.Cli -- master --port COM1 --baud 9600 --link 1 --ca 1 --duration 60 --mapping samples\mapping-profiles\example-user-mapping.profile.json --report out\master-vs-slave.md --json out\master-vs-slave.json
```

## Protection relay behavior model

The simulator now behaves like a simple latched protection relay:

1. GI returns a snapshot of pickup/trip states, animated currents, identification, and GI END.
2. During normal Class 2 polling, the simulator returns animated current measurands when no Class 1 event is pending.
3. A protection pickup is generated after the configured initial delay. The phase is random among A, B, and V/C.
4. About 200 ms after pickup, the trip signal for the same phase is generated.
5. Pickup and trip remain ON until reset.
6. Reset happens either by:
   - IEC-103 command ASDU Type 20 with configured reset address, default FUN=255 / INF=19, or
   - auto reset after 20 seconds.
7. After reset, the next pickup/trip cycle starts after 10 seconds.

This model is intended for master-engine validation, not as an official vendor relay model.

## Default simulator signal map

The editable sample mapping uses these simulator points:

| FUN | INF | Signal |
|---:|---:|---|
| 160 | 1 | Protection Pickup Phase A |
| 160 | 2 | Protection Pickup Phase B |
| 160 | 3 | Protection Pickup Phase V/C |
| 161 | 1 | Protection Trip Phase A |
| 161 | 2 | Protection Trip Phase B |
| 161 | 3 | Protection Trip Phase V/C |
| 144 | 1 | Phase A Current |
| 144 | 2 | Phase B Current |
| 144 | 3 | Phase V/C Current |

The current values are demo values encoded as centi-ampere. The sample mapping applies `scale = 0.01` and unit `A`.

## Timing options

```bat
--initial-fault-delay 3    First pickup delay in seconds.
--trip-delay 200           Delay from pickup ON to trip ON in milliseconds.
--auto-reset 20            Auto reset delay in seconds.
--fault-repeat-delay 10    Delay from reset to next pickup/trip cycle in seconds.
--random-seed 103          Deterministic random seed for phase selection.
--reset-fun 255            Reset command FUN address.
--reset-inf 19             Reset command INF address.
--no-protection-demo       Disable protection pickup/trip behavior.
```

Example fast demo:

```bat
dotnet run --project src\ArIEC103.Cli -- slave --port COM2 --initial-fault-delay 2 --trip-delay 200 --auto-reset 8 --fault-repeat-delay 4
```

## Negative test modes

```bat
--missing-gi-end   Simulate relay that never sends GI END.
--dfc-busy         Set DFC=1 to validate master busy/backoff handling.
--silent           Do not respond; useful for timeout/recovery testing.
--bad-checksum     Corrupt response checksums.
--no-spontaneous   Legacy flag retained for compatibility; protection behavior is controlled by --no-protection-demo.
```

Example:

```bat
dotnet run --project src\ArIEC103.Cli -- slave --port COM2 --missing-gi-end
```

## Protocol behavior rules

The simulator must preserve IEC-103 master/slave behavior:

- It raises ACD=1 only when Class 1 event data is pending.
- It returns Class 1 user data only when the master requests Class 1.
- It returns NO DATA for empty Class 1 requests.
- It uses Class 2 responses for background measurands only when no Class 1 queue is pending.
- It never pushes data without master polling.

## Product boundary

The simulator is not intended to become the main product. The main product remains:

> ArIEC103 = IEC-103 Active Master Tester + Analyzer.

The simulator exists to make the master testable, deterministic, and regression-friendly.
