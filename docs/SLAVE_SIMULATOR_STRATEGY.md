# ArIEC103 IEC-103 Slave Simulator Strategy

## Why the simulator is needed

ArIEC103 is an active IEC-103 master tester. A master tester cannot be hardened properly if every test depends on a physical relay on the bench. A controlled slave simulator is required for repeatable engine tests, WPF workflow validation, regression testing, and FAT/SAT scenario development.

The simulator is not the main product direction. It is a supporting test and demo environment that makes the master reliable.

## Product boundary

ArIEC103 remains:

```text
IEC-103 Active Master Tester + Analyzer
```

The slave simulator exists to validate the master:

```text
ArIEC103 Master  <---- FT1.2 / Serial / TCP test channel ---->  ArIEC103 Slave Simulator
```

## Two simulator layers

### 1. In-process simulated relay transport

Purpose:

- quick demo mode
- CLI regression
- WPF smoke test without hardware
- deterministic evidence generation

This already exists as `SimulatedRelayTransport`, but it must remain small and deterministic. It should not become a full relay model.

### 2. Standalone IEC-103 slave simulator

Purpose:

- test the real master state machine through the same transport abstraction used for physical relays
- test serial timing, timeout, FCB/FCV, ACD, DFC, GI, Class 1 drain, and NO DATA behavior
- allow automated scenarios and negative tests

Recommended new project:

```text
src/ArIEC103.Simulator
```

Optional UI later:

```text
src/ArIEC103.Simulator.Desktop
```

## Simulator architecture

```text
SlaveScenario
  ↓
SlaveStateMachine
  ↓
Class1Queue / Class2Queue
  ↓
FT1.2 Frame Builder
  ↓
Transport Adapter
  ↓
Master Under Test
```

Core components:

```text
ArIEC103.Simulator
├─ Iec103SlaveSimulatorSession
├─ Iec103SlaveSimulatorOptions
├─ Iec103SlaveState
├─ Iec103SlaveScenario
├─ Iec103SlaveScenarioStep
├─ Iec103SlaveResponsePlanner
├─ Iec103SlaveEventQueue
├─ Iec103SlaveValueStore
└─ Iec103SlaveEvidenceEvent
```

## Minimum behavior for v1.3

The first standalone simulator should support:

- open transport as IEC-103 slave endpoint
- configured link address and common address
- respond to Reset FCB / Reset Remote Link with ACK
- respond to Clock Sync with ACK or mirrored confirmation
- respond to General Interrogation with ACK, then set ACD=1
- Class 1 queue for GI response events
- Class 2 queue/background response
- send Type 1 / Type 2 DPI events
- send Type 5 identification
- send Type 8 GI termination
- return NO DATA when queue is empty
- set ACD=1 only when Class 1 queue has pending event data
- clear ACD when Class 1 queue is drained

This is enough to verify that ArIEC103 master does not bombard Class 1 and uses Class 2 as normal polling.

## Scenario file concept

A simulator scenario should be user-editable JSON, not hardcoded vendor profile.

Example:

```json
{
  "schema": "ariec103-slave-scenario-v1",
  "name": "Basic GI and Class 1 Drain",
  "linkAddress": 1,
  "commonAddress": 1,
  "identification": {
    "manufacturer": "DEMO",
    "device": "Generic IEC-103 Relay"
  },
  "initialValues": [
    {
      "type": "DPI",
      "fun": 192,
      "inf": 36,
      "dpi": 1,
      "timeMode": "relay-current-time"
    }
  ],
  "events": [
    {
      "trigger": "after-gi",
      "type": "DPI_RT",
      "fun": 150,
      "inf": 180,
      "dpi": 2,
      "delayMs": 50
    },
    {
      "trigger": "manual",
      "name": "Trip event",
      "type": "DPI",
      "fun": 150,
      "inf": 181,
      "dpi": 2
    }
  ]
}
```

The simulator should not assign official signal names. Signal naming belongs to the user mapping profile system.

## Negative behavior modes

To harden the master, simulator must later support bad behavior profiles:

```text
- delayed response
- no response / timeout
- DFC busy
- ACD stuck high
- GI without GI END
- invalid relay timestamp
- bad checksum
- malformed length
- wrong link address
- FCB mismatch
- repeated NO DATA
- unknown/private ASDU
```

Each bad behavior mode should produce evidence so master findings can be tested deterministically.

## Transport strategy

### Phase 1: In-memory simulator

Used by CLI and WPF demo mode.

```text
MasterSession -> SimulatedRelayTransport -> SlaveResponsePlanner
```

### Phase 2: TCP simulator

Recommended first standalone transport because it is easy to automate without physical null-modem hardware.

```text
Master TCP client/test adapter -> TCP Slave Simulator
```

### Phase 3: Serial simulator

Use virtual COM pair on Windows for development testing, and physical RS-232/RS-485 loop for hardware behavior.

Typical Windows virtual COM options are developer environment concerns and must not be bundled as dependencies.

## Acceptance tests for master using simulator

The master passes simulator validation when:

- startup completes without stuck Start/Stop UI
- Reset FCB receives ACK
- GI is sent and GI termination is received
- Class 2 is used as normal background polling
- Class 1 is requested only during bounded GI follow-up or when ACD=1
- NO DATA stops Class 1 drain
- DFC triggers backoff
- relay timestamp is used for Relay Event Log
- state-change events are recorded once, not duplicated by every GI snapshot
- Value Viewer updates current state without noisy event spam
- raw Frame Trace still records every retained TX/RX frame

## Roadmap position

A deterministic slave simulator provides earlier regression protection than advanced PDF reporting because it exercises the master engine under repeatable scenarios.

Recommended order:

```text
v1.2.1  Compile fix + simulator strategy
v1.3    Standalone basic slave simulator
v1.4    Master hardening using simulator scenarios
v1.5    AutoTest/FAT scenario runner
v1.6    Professional HTML/PDF reports
```

